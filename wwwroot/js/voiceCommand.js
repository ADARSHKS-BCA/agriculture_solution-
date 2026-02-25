document.addEventListener('DOMContentLoaded', () => {
    // Check if the browser supports Speech Recognition
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;

    if (!SpeechRecognition) {
        console.warn('Speech Recognition API is not supported in this browser.');
        const btn = document.getElementById('btnVoiceCommand');
        if (btn) {
            btn.style.display = 'none'; // Hide the button if not supported
        }
        return;
    }

    const recognition = new SpeechRecognition();
    recognition.continuous = true; // Keep listening
    recognition.interimResults = false;
    recognition.lang = 'en-US';

    let isListening = false;
    const btnVoiceCommand = document.getElementById('btnVoiceCommand');
    const iconVoiceCommand = document.getElementById('iconVoiceCommand');

    // Attempt to load previous state
    const savedState = sessionStorage.getItem('voiceCommandState');
    if (savedState === 'listening') {
        startListening();
    }

    btnVoiceCommand.addEventListener('click', (e) => {
        e.preventDefault();
        if (isListening) {
            stopListening();
        } else {
            startListening();
        }
    });

    function startListening() {
        if (isListening) return;
        try {
            recognition.start();
            isListening = true;
            sessionStorage.setItem('voiceCommandState', 'listening');
            updateUI(true);
            showToast('Voice Command', 'Microphone is now active. Try saying "Scan Leaf" or "Dashboard".', 'success');
        } catch (err) {
            console.error('Error starting recognition:', err);
        }
    }

    function stopListening() {
        if (!isListening) return;
        try {
            recognition.stop();
            isListening = false;
            sessionStorage.setItem('voiceCommandState', 'idle');
            updateUI(false);
            showToast('Voice Command', 'Microphone paused.', 'secondary');
        } catch (err) {
            console.error('Error stopping recognition:', err);
        }
    }

    function updateUI(active) {
        if (active) {
            btnVoiceCommand.classList.remove('btn-outline-dark');
            btnVoiceCommand.classList.add('btn-danger'); // Red to indicate recording
            iconVoiceCommand.classList.remove('bi-mic');
            iconVoiceCommand.classList.add('bi-mic-fill', 'pulse-animation');
            btnVoiceCommand.setAttribute('title', 'Listening... Click to stop.');
        } else {
            btnVoiceCommand.classList.remove('btn-danger');
            btnVoiceCommand.classList.add('btn-outline-dark');
            iconVoiceCommand.classList.remove('bi-mic-fill', 'pulse-animation');
            iconVoiceCommand.classList.add('bi-mic');
            btnVoiceCommand.setAttribute('title', 'Start Voice Commands');
        }
    }

    // Auto-restart if it stops unexpectedly while we still want to be listening
    recognition.addEventListener('end', () => {
        if (isListening) {
            try {
                recognition.start();
            } catch (err) {
                console.error("Auto-restart failed", err);
                stopListening();
            }
        }
    });

    recognition.addEventListener('error', (event) => {
        console.error('Speech recognition error:', event.error);
        if (event.error === 'not-allowed' || event.error === 'service-not-allowed') {
            stopListening();
            showToast('Voice Command Error', 'Microphone permission denied.', 'danger');
        }
    });

    recognition.addEventListener('result', (event) => {
        // Only look at the latest result
        const currentResultIndex = event.results.length - 1;
        const transcript = event.results[currentResultIndex][0].transcript.trim().toLowerCase();

        console.log('Recognized text:', transcript);

        // Form Handling on the Scan page
        const isScanPage = window.location.pathname.toLowerCase().includes('/scan/index') ||
            window.location.pathname.toLowerCase().endsWith('/scan');

        if (isScanPage) {
            // Set Crop (Relaxed regex to catch more variations)
            // Examples: "crop is tomato", "select potato", "set crop to wheat", "crop tomato"
            const cropMatch = transcript.match(/(?:crop|select(?: crop)?|set(?: crop)?(?: to)?)\s*(tomato|potato|corn|wheat|rice)/i);
            if (cropMatch) {
                const cropInput = cropMatch[1].trim();
                const cropSelect = document.getElementById('cropSelect');
                if (cropSelect) {
                    for (let i = 0; i < cropSelect.options.length; i++) {
                        if (cropSelect.options[i].text.toLowerCase() === cropInput.toLowerCase()) {
                            cropSelect.selectedIndex = i;
                            showToast('Voice Command', `Crop set to ${cropSelect.options[i].text}`, 'success');
                            break;
                        }
                    }
                }
                return; // Stop processing further commands
            }

            // Set City/Location (Relaxed regex to catch more variations)
            // Examples: "city is london", "location paris", "set city to bangalore", "city mumbai"
            const cityMatch = transcript.match(/(?:city(?: is|:)?|location(?: is|:)?|set(?: city| location)?(?: to)?)\s+([a-zA-Z\s]+)/i);
            if (cityMatch) {
                let cityInputVal = cityMatch[1].trim();
                // Sometimes it picks up trailing words like 'analyze', clean it
                cityInputVal = cityInputVal.replace(/(analyze|start|scan|submit|form).*$/i, '').trim();

                const cityEl = document.getElementById('cityInput');
                if (cityEl && cityInputVal.length > 0) {
                    cityEl.value = cityInputVal;
                    showToast('Voice Command', `Location set to ${cityInputVal}`, 'success');
                }
                return;
            }

            // Trigger Analysis (e.g. "Analyze it", "Submit form", "Start scan")
            if (transcript.includes('analyze') || transcript.includes('start scan') || transcript.includes('submit form')) {
                const formEl = document.getElementById('analysisForm');
                if (formEl) {
                    showToast('Voice Command', 'Starting Analysis...', 'primary');
                    formEl.requestSubmit(); // Better than .submit() as it triggers validation and event listeners
                }
                return;
            }
        }

        // Global Navigation Command mapping
        if (!isScanPage && (transcript.includes('scan leaf') || transcript.includes('new scan') || transcript.includes('scan'))) {
            showToast('Voice Command', 'Navigating to New Scan...', 'primary');
            window.location.href = '/Scan/Index';
        } else if (transcript.includes('check profit') || transcript.includes('add expense') || transcript.includes('wallet')) {
            showToast('Voice Command', 'Navigating to Farm Wallet...', 'primary');
            window.location.href = '/FarmWallet/Index';
        } else if (transcript.includes('dashboard') || transcript.includes('home')) {
            showToast('Voice Command', 'Navigating to Dashboard...', 'primary');
            window.location.href = '/Dashboard/Index';
        } else if (transcript.includes('stop listening') || transcript.includes('turn off microphone')) {
            stopListening();
        }
    });

    // Simple toast helper function since we have a toast container
    function showToast(title, message, type) {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const toastId = 'toast-' + Date.now();
        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-bg-${type} border-0 mb-3 show" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="4000">
                <div class="d-flex">
                    <div class="toast-body">
                        <strong>${title}</strong><br/>
                        ${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>`;

        container.insertAdjacentHTML('beforeend', toastHtml);

        // Auto-remove after 4 seconds
        setTimeout(() => {
            const el = document.getElementById(toastId);
            if (el) {
                el.classList.remove('show');
                setTimeout(() => el.remove(), 300); // Wait for transition
            }
        }, 4000);
    }
});
