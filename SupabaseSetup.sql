-- 1. Modify existing `profiles` table to include `username`
ALTER TABLE public.profiles ADD COLUMN IF NOT EXISTS username text;

-- 2. Drop existing trigger and function if you are updating them
DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;
DROP FUNCTION IF EXISTS public.handle_new_user();

-- 3. Create the new function to copy standard data AND the custom `username` metadata
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
  INSERT INTO public.profiles (id, email, username)
  VALUES (
    NEW.id,
    NEW.email,
    NEW.raw_user_meta_data->>'username' -- Extracts the username we passed during AuthController's SignUp options
  );
  RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- 4. Re-attach the trigger
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW EXECUTE PROCEDURE public.handle_new_user();

-- 5. Ensure RLS is active
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.analysis_history ENABLE ROW LEVEL SECURITY;

-- Profiles: Users can only read/update their OWN profile
CREATE POLICY "Users can view own profile" 
ON public.profiles FOR SELECT USING (auth.uid() = id);

CREATE POLICY "Users can update own profile" 
ON public.profiles FOR UPDATE USING (auth.uid() = id);

-- Analysis History: Users can only insert/select their OWN history
CREATE POLICY "Users can insert own history" 
ON public.analysis_history FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can view own history" 
ON public.analysis_history FOR SELECT USING (auth.uid() = user_id);
