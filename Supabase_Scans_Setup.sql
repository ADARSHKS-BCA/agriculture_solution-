-- 1. Create the Scans Table
CREATE TABLE public.scans (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    risk_level TEXT NOT NULL CHECK (risk_level IN ('High', 'Moderate', 'Low')),
    crop_type TEXT NOT NULL,
    result_summary TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW() NOT NULL
);

-- 2. Enable Row Level Security (RLS)
ALTER TABLE public.scans ENABLE ROW LEVEL SECURITY;

-- 3. Create RLS Policies
-- Users can only insert their own scans
CREATE POLICY "Users can insert own scans" 
ON public.scans 
FOR INSERT 
WITH CHECK (auth.uid() = user_id);

-- Users can only view their own scans
CREATE POLICY "Users can view own scans" 
ON public.scans 
FOR SELECT 
USING (auth.uid() = user_id);

-- Users can only delete their own scans
CREATE POLICY "Users can delete own scans" 
ON public.scans 
FOR DELETE 
USING (auth.uid() = user_id);
