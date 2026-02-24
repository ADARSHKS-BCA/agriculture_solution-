/* ==================================================================
   AgriShield — Site JavaScript
   Drag-and-drop, validation, loading states, toasts, animations
   ================================================================== */

document.addEventListener('DOMContentLoaded', () => {

    // ---- Elements ----
    const dropZone = document.getElementById('dropZone');
    const fileInput = document.getElementById('imageInput');
    const preview = document.getElementById('imagePreview');
    const dropPrompt = document.getElementById('dropPrompt');
    const removeBtn = document.getElementById('removeImage');
    const fileError = document.getElementById('fileError');
    const form = document.getElementById('analysisForm');
    const submitBtn = document.getElementById('submitBtn');
    const submitText = document.getElementById('submitText');
    const submitSpinner = document.getElementById('submitSpinner');
    const overlay = document.getElementById('loadingOverlay');

    const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5 MB
    const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/jpg', 'image/webp'];

    // ---- Drag & Drop ----
    if (dropZone) {
        ['dragenter', 'dragover'].forEach(evt => {
            dropZone.addEventListener(evt, e => {
                e.preventDefault();
                dropZone.classList.add('drag-over');
            });
        });

        ['dragleave', 'drop'].forEach(evt => {
            dropZone.addEventListener(evt, e => {
                e.preventDefault();
                dropZone.classList.remove('drag-over');
            });
        });

        dropZone.addEventListener('drop', e => {
            const file = e.dataTransfer.files[0];
            if (file) {
                handleFile(file);
                // Sync to input
                const dt = new DataTransfer();
                dt.items.add(file);
                fileInput.files = dt.files;
            }
        });
    }

    // ---- File Input Change ----
    if (fileInput) {
        fileInput.addEventListener('change', () => {
            if (fileInput.files.length > 0) {
                handleFile(fileInput.files[0]);
            }
        });
    }

    // ---- Remove Image ----
    if (removeBtn) {
        removeBtn.addEventListener('click', e => {
            e.preventDefault();
            e.stopPropagation();
            clearFile();
        });
    }

    function handleFile(file) {
        hideError();

        // Validate type
        if (!ALLOWED_TYPES.includes(file.type)) {
            showError('Please upload a valid image file (PNG, JPG, JPEG, WEBP).');
            clearFile();
            return;
        }

        // Validate size
        if (file.size > MAX_FILE_SIZE) {
            showError(`File size (${(file.size / 1024 / 1024).toFixed(1)} MB) exceeds the 5 MB limit.`);
            clearFile();
            return;
        }

        // Show preview
        const reader = new FileReader();
        reader.onload = e => {
            preview.src = e.target.result;
            preview.classList.remove('d-none');
            dropPrompt.classList.add('d-none');
            removeBtn.classList.remove('d-none');
        };
        reader.readAsDataURL(file);
    }

    function clearFile() {
        if (fileInput) fileInput.value = '';
        if (preview) {
            preview.src = '';
            preview.classList.add('d-none');
        }
        if (dropPrompt) dropPrompt.classList.remove('d-none');
        if (removeBtn) removeBtn.classList.add('d-none');
    }

    function showError(msg) {
        if (fileError) {
            fileError.textContent = msg;
            fileError.classList.remove('d-none');
        }
    }

    function hideError() {
        if (fileError) {
            fileError.textContent = '';
            fileError.classList.add('d-none');
        }
    }

    // ---- Form Submission ----
    if (form) {
        form.addEventListener('submit', e => {
            hideError();

            // Client-side validation
            if (!fileInput || fileInput.files.length === 0) {
                e.preventDefault();
                showError('Please select or drop a crop leaf image.');
                return;
            }

            const file = fileInput.files[0];
            if (!ALLOWED_TYPES.includes(file.type)) {
                e.preventDefault();
                showError('Invalid file type. Please upload a PNG, JPG, or WEBP image.');
                return;
            }

            if (file.size > MAX_FILE_SIZE) {
                e.preventDefault();
                showError(`File too large (${(file.size / 1024 / 1024).toFixed(1)} MB). Max is 5 MB.`);
                return;
            }

            // Show loading state
            if (submitBtn) submitBtn.disabled = true;
            if (submitText) submitText.classList.add('d-none');
            if (submitSpinner) submitSpinner.classList.remove('d-none');
            if (overlay) overlay.classList.remove('d-none');
        });
    }

    // ---- Geolocation Handling ----
    const btnGeo = document.getElementById('btnGeolocation');
    const cityInput = document.getElementById('cityInput');
    const latInput = document.getElementById('latInput');
    const lonInput = document.getElementById('lonInput');
    const geoStatus = document.getElementById('geoStatus');

    if (btnGeo) {
        btnGeo.addEventListener('click', () => {
            if (!navigator.geolocation) {
                updateGeoStatus("Geolocation is not supported by your browser.", "text-danger");
                return;
            }

            updateGeoStatus("Locating...", "text-info");
            btnGeo.disabled = true;

            navigator.geolocation.getCurrentPosition(
                async (position) => {
                    const lat = position.coords.latitude;
                    const lon = position.coords.longitude;

                    if (latInput) latInput.value = lat;
                    if (lonInput) lonInput.value = lon;

                    updateGeoStatus(`Coordinates acquired: ${lat.toFixed(4)}, ${lon.toFixed(4)}`, "text-success");

                    // Attempt reverse geocoding to fill the city name for user convenience
                    try {
                        const response = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lon}&zoom=10&addressdetails=1`, {
                            headers: {
                                'Accept-Language': 'en'
                            }
                        });

                        if (response.ok) {
                            const data = await response.json();
                            const actCity = data.address.city || data.address.town || data.address.village || data.address.county || "";
                            if (actCity && cityInput) {
                                cityInput.value = actCity;
                            }
                        }
                    } catch (err) {
                        console.warn("Reverse geocoding failed", err);
                    }

                    btnGeo.disabled = false;
                },
                (error) => {
                    let msg = "Unable to retrieve location.";
                    if (error.code === error.PERMISSION_DENIED) msg = "Location access denied by user.";
                    else if (error.code === error.POSITION_UNAVAILABLE) msg = "Location information unavailable.";
                    else if (error.code === error.TIMEOUT) msg = "Location request timed out.";

                    updateGeoStatus(msg, "text-danger");
                    btnGeo.disabled = false;
                },
                { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
            );
        });
    }

    function updateGeoStatus(message, className) {
        if (!geoStatus) return;
        geoStatus.textContent = message;
        geoStatus.className = `small mt-1 ${className}`;
        geoStatus.classList.remove('d-none');
    }

});

// ---- Toast Notification ----
function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;

    const id = 'toast-' + Date.now();
    const icon = type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';

    const html = `
        <div id="${id}" class="toast toast-agri toast-${type} align-items-center border-0" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body d-flex align-items-center gap-2">
                    <i class="bi ${icon}"></i>
                    <span>${message}</span>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', html);

    const toastEl = document.getElementById(id);
    const bsToast = new bootstrap.Toast(toastEl, { delay: 4000 });
    bsToast.show();

    toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
}

// ---- API Error Alert ----
function showApiError(message) {
    const main = document.querySelector('main');
    if (!main) return;

    const alert = document.createElement('div');
    alert.className = 'alert alert-api-error alert-dismissible fade show mx-3 mt-3';
    alert.innerHTML = `
        <div class="d-flex align-items-center gap-2">
            <i class="bi bi-exclamation-octagon-fill"></i>
            <strong>Error:</strong> ${message}
        </div>
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;
    main.insertBefore(alert, main.firstChild);
}
