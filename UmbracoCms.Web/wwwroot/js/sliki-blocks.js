const uploaderStates = new Map();
const galleryStates = new Map();

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-sliki-uploader]").forEach(initializeUploader);
    document.querySelectorAll("[data-sliki-gallery]").forEach(initializeGallery);
});

window.addEventListener("sliki:images-updated", () => {
    for (const state of galleryStates.values()) {
        loadGalleryPage(state, 1);
    }
});

function initializeUploader(root) {
    if (!root || uploaderStates.has(root.id)) return;
    const state = {
        root,
        input: root.querySelector("[data-upload-input]"),
        uploadAllButton: root.querySelector("[data-upload-action='upload-all']"),
        retryFailedButton: root.querySelector("[data-upload-action='retry-failed']"),
        clearSelectionButton: root.querySelector("[data-upload-action='clear-selection']"),
        statusMessage: root.querySelector("[data-upload-status]"),
        emptyState: root.querySelector("[data-upload-empty-state]"),
        list: root.querySelector("[data-upload-list]"),
        uploadUrl: root.dataset.uploadUrl || "",
        maxUploadBytes: Number(root.dataset.maxUploadBytes || "0"),
        acceptedMimeTypes: (root.dataset.acceptedMimeTypes || "").split(",").filter(Boolean),
        items: []
    };
    if (!state.input || !state.uploadAllButton || !state.retryFailedButton || !state.clearSelectionButton || !state.statusMessage || !state.emptyState || !state.list) return;
    state.input.addEventListener("change", () => handleSelection(state));
    state.uploadAllButton.addEventListener("click", () => uploadMatching(state, item => item.state === "pending" || item.state === "failed"));
    state.retryFailedButton.addEventListener("click", () => uploadMatching(state, item => item.state === "failed"));
    state.clearSelectionButton.addEventListener("click", () => clearSelection(state));
    uploaderStates.set(root.id, state);
    renderUploader(state);
}

function handleSelection(state) {
    setText(state.statusMessage, "");
    const files = Array.from(state.input.files || []);
    if (!files.length) { renderUploader(state); return; }
    const allowedMimeTypes = new Set(state.acceptedMimeTypes);
    revokePreviewUrls(state.items);
    state.items = files.map(file => {
        const item = { id: crypto.randomUUID(), file, displayName: file.name, browserContentType: file.type || "unknown", sizeInBytes: file.size, previewUrl: file.type.startsWith("image/") ? URL.createObjectURL(file) : null, state: "pending", progressPercent: 0, statusDetails: "Ready to upload.", errorMessage: "" };
        if (file.size > state.maxUploadBytes) {
            item.state = "failed"; item.statusDetails = "File rejected before upload."; item.errorMessage = `The file exceeds the limit of ${formatFileSize(state.maxUploadBytes)}.`;
        } else if (file.type && !allowedMimeTypes.has(file.type)) {
            item.state = "failed"; item.statusDetails = "File rejected before upload."; item.errorMessage = "The selected file type is not allowed.";
        }
        return item;
    });
    renderUploader(state);
}

async function uploadMatching(state, predicate) {
    const targets = state.items.filter(predicate);
    if (!targets.length) return;
    setText(state.statusMessage, "");
    let uploadedCount = 0;
    for (const item of targets) {
        const uploaded = await uploadItem(state, item);
        if (uploaded) uploadedCount += 1;
        renderUploader(state);
    }
    if (uploadedCount > 0) {
        setText(state.statusMessage, uploadedCount === 1 ? "1 image uploaded successfully." : `${uploadedCount} images uploaded successfully.`);
        window.dispatchEvent(new CustomEvent("sliki:images-updated"));
    }
}

function uploadItem(state, item) {
    return new Promise(resolve => {
        item.state = "uploading"; item.progressPercent = 0; item.statusDetails = "Uploading to Azure Blob Storage..."; item.errorMessage = ""; renderUploader(state);
        const formData = new FormData(); formData.append("file", item.file, item.file.name);
        const request = new XMLHttpRequest(); request.open("POST", state.uploadUrl, true);
        request.upload.addEventListener("progress", event => { if (!event.lengthComputable) return; item.progressPercent = Math.max(1, Math.min(100, Math.round((event.loaded / event.total) * 100))); renderUploader(state); });
        request.addEventListener("load", () => {
            if (request.status >= 200 && request.status < 300) { item.state = "success"; item.progressPercent = 100; item.statusDetails = "Uploaded successfully."; resolve(true); return; }
            item.state = "failed"; item.progressPercent = 0; item.statusDetails = "Upload failed."; item.errorMessage = parseErrorMessage(request.responseText); resolve(false);
        });
        request.addEventListener("error", () => { item.state = "failed"; item.progressPercent = 0; item.statusDetails = "Upload failed."; item.errorMessage = "The upload request could not reach the server."; resolve(false); });
        request.send(formData);
    });
}

function clearSelection(state) { revokePreviewUrls(state.items); state.items = []; state.input.value = ""; setText(state.statusMessage, ""); renderUploader(state); }

function renderUploader(state) {
    state.emptyState.classList.toggle("is-hidden", state.items.length > 0);
    state.list.classList.toggle("is-hidden", state.items.length === 0);
    const canUploadAny = state.items.some(item => item.state === "pending" || item.state === "failed");
    const hasFailedItems = state.items.some(item => item.state === "failed");
    const isUploading = state.items.some(item => item.state === "uploading");
    state.uploadAllButton.disabled = !canUploadAny || isUploading;
    state.retryFailedButton.disabled = !hasFailedItems || isUploading;
    state.clearSelectionButton.disabled = state.items.length === 0 || isUploading;
    if (state.items.length === 0) { state.list.innerHTML = ""; return; }
    state.list.innerHTML = state.items.map(item => `
        <article class="sliki-upload-item">
            <div class="sliki-upload-item__preview">${item.previewUrl ? `<img src="${item.previewUrl}" alt="" />` : `<div class="sliki-upload-item__placeholder" aria-hidden="true">IMG</div>`}</div>
            <div>
                <div class="sliki-upload-item__header">
                    <div><h3>${escapeHtml(item.displayName)}</h3><p class="sliki-upload-item__meta">${formatFileSize(item.sizeInBytes)} • ${escapeHtml(item.browserContentType)}</p></div>
                    <span class="sliki-badge sliki-badge--${getUploadBadgeModifier(item.state)}">${getUploadLabel(item.state)}</span>
                </div>
                <div class="sliki-progress" role="progressbar" aria-label="Upload progress" aria-valuenow="${item.progressPercent}" aria-valuemin="0" aria-valuemax="100"><div class="sliki-progress__bar" style="width:${item.progressPercent}%">${item.progressPercent}%</div></div>
                <p class="sliki-upload-item__status">${escapeHtml(item.statusDetails)}</p>
                ${item.errorMessage ? `<p class="sliki-error">${escapeHtml(item.errorMessage)}</p>` : ""}
            </div>
            <div><button type="button" class="c-button c-button--secondary" data-upload-item="${item.id}" ${item.state === "uploading" ? "disabled" : ""}><span class="button__label">${item.state === "failed" ? "Retry" : "Upload"}</span></button></div>
        </article>`).join("");
    state.list.querySelectorAll("[data-upload-item]").forEach(button => {
        button.addEventListener("click", async event => {
            const itemId = event.currentTarget.getAttribute("data-upload-item");
            const item = state.items.find(candidate => candidate.id === itemId);
            if (!item || item.state === "uploading") return;
            const uploaded = await uploadItem(state, item);
            renderUploader(state);
            if (uploaded) { setText(state.statusMessage, "1 image uploaded successfully."); window.dispatchEvent(new CustomEvent("sliki:images-updated")); }
        }, { once: true });
    });
}

function initializeGallery(root) {
    if (!root || galleryStates.has(root.id)) return;
    const state = {
        root, listUrl: root.dataset.listUrl || "", imageUrlTemplate: root.dataset.imageUrlTemplate || "", pageSize: Number(root.dataset.pageSize || "12"),
        searchInput: root.querySelector("[data-gallery-search]"), sortInput: root.querySelector("[data-gallery-sort]"), applyButton: root.querySelector("[data-gallery-apply]"), resetButton: root.querySelector("[data-gallery-reset]"),
        error: root.querySelector("[data-gallery-error]"), summary: root.querySelector("[data-gallery-summary]"), emptyState: root.querySelector("[data-gallery-empty-state]"), grid: root.querySelector("[data-gallery-grid]"),
        pagination: root.querySelector("[data-gallery-pagination]"), prevButton: root.querySelector("[data-gallery-prev]"), nextButton: root.querySelector("[data-gallery-next]"), pageLabel: root.querySelector("[data-gallery-page-label]"),
        lightbox: root.querySelector("[data-gallery-lightbox]"), lightboxTitle: root.querySelector("[data-gallery-lightbox-title]"), lightboxMeta: root.querySelector("[data-gallery-lightbox-meta]"), lightboxImage: root.querySelector("[data-gallery-lightbox-image]"),
        currentPage: 1, totalPages: 1
    };
    if (!state.searchInput || !state.sortInput || !state.applyButton || !state.resetButton || !state.error || !state.summary || !state.emptyState || !state.grid || !state.pagination || !state.prevButton || !state.nextButton || !state.pageLabel || !state.lightbox || !state.lightboxTitle || !state.lightboxMeta || !state.lightboxImage) return;
    state.applyButton.addEventListener("click", () => loadGalleryPage(state, 1));
    state.resetButton.addEventListener("click", () => { state.searchInput.value = ""; state.sortInput.value = "LatestFirst"; loadGalleryPage(state, 1); });
    state.prevButton.addEventListener("click", () => loadGalleryPage(state, Math.max(1, state.currentPage - 1)));
    state.nextButton.addEventListener("click", () => loadGalleryPage(state, Math.min(state.totalPages, state.currentPage + 1)));
    root.querySelectorAll("[data-gallery-lightbox-close]").forEach(element => element.addEventListener("click", () => closeLightbox(state)));
    galleryStates.set(root.id, state);
    loadGalleryPage(state, 1);
}

async function loadGalleryPage(state, pageNumber) {
    try {
        setText(state.error, ""); state.grid.innerHTML = "";
        const url = new URL(state.listUrl, window.location.origin);
        url.searchParams.set("searchTerm", state.searchInput.value.trim());
        url.searchParams.set("sortBy", state.sortInput.value);
        url.searchParams.set("pageNumber", String(pageNumber));
        url.searchParams.set("pageSize", String(state.pageSize));
        const response = await fetch(url.toString(), { headers: { Accept: "application/json" } });
        const payload = await response.json();
        if (!response.ok) throw new Error(payload.error || "The gallery could not be loaded.");
        state.currentPage = payload.pageNumber; state.totalPages = payload.totalPages; renderGallery(state, payload);
    } catch (error) {
        state.emptyState.classList.add("is-hidden"); state.pagination.classList.add("is-hidden"); state.summary.classList.add("is-hidden"); setText(state.error, error.message || "The gallery could not be loaded.");
    }
}

function renderGallery(state, payload) {
    const items = payload.items || [];
    state.emptyState.classList.toggle("is-hidden", items.length > 0);
    state.pagination.classList.toggle("is-hidden", items.length === 0);
    state.summary.classList.toggle("is-hidden", items.length === 0);
    if (!items.length) { state.grid.innerHTML = ""; state.pageLabel.textContent = ""; return; }
    state.summary.textContent = `${payload.totalCount} image${payload.totalCount === 1 ? "" : "s"} found`;
    state.pageLabel.textContent = `Page ${payload.pageNumber} of ${payload.totalPages}`;
    state.prevButton.disabled = payload.pageNumber <= 1;
    state.nextButton.disabled = payload.pageNumber >= payload.totalPages;
    state.grid.innerHTML = items.map(item => {
        const imageUrl = state.imageUrlTemplate.replace("__BLOB__", encodeURIComponent(item.blobName));
        return `<button type="button" class="sliki-gallery__card" data-gallery-open="${escapeHtmlAttribute(encodeURIComponent(JSON.stringify({ imageUrl, fileName: item.fileName, uploadedAt: item.uploadedAt, sizeInBytes: item.sizeInBytes })))}"><img src="${imageUrl}" alt="${escapeHtmlAttribute(item.fileName)}" loading="lazy" /><span class="sliki-gallery__card-body"><strong>${escapeHtml(item.fileName)}</strong><span>${formatDate(item.uploadedAt)}</span><span>${formatFileSize(item.sizeInBytes)}</span></span></button>`;
    }).join("");
    state.grid.querySelectorAll("[data-gallery-open]").forEach(button => button.addEventListener("click", event => { const raw = event.currentTarget.getAttribute("data-gallery-open"); if (!raw) return; openLightbox(state, JSON.parse(decodeURIComponent(raw))); }));
}

function openLightbox(state, image) { state.lightboxImage.src = image.imageUrl; state.lightboxImage.alt = image.fileName; state.lightboxTitle.textContent = image.fileName; state.lightboxMeta.textContent = `${formatDate(image.uploadedAt)} • ${formatFileSize(image.sizeInBytes)}`; state.lightbox.classList.remove("is-hidden"); }
function closeLightbox(state) { state.lightbox.classList.add("is-hidden"); state.lightboxImage.src = ""; }
function revokePreviewUrls(items) { items.forEach(item => { if (item.previewUrl) URL.revokeObjectURL(item.previewUrl); }); }
function getUploadBadgeModifier(state) { switch (state) { case "success": return "success"; case "uploading": return "uploading"; case "failed": return "failed"; default: return "ready"; } }
function getUploadLabel(state) { switch (state) { case "success": return "Uploaded"; case "uploading": return "Uploading"; case "failed": return "Failed"; default: return "Ready"; } }
function parseErrorMessage(responseText) { try { const parsed = JSON.parse(responseText); return parsed.error || "The upload failed."; } catch { return responseText || "The upload failed."; } }
function setText(element, text) { element.textContent = text; element.classList.toggle("is-hidden", !text); }
function formatFileSize(value) { if (value < 1024) return `${value} bytes`; const units = ["KB", "MB", "GB"]; let size = value / 1024; let unitIndex = 0; while (size >= 1024 && unitIndex < units.length - 1) { size /= 1024; unitIndex += 1; } return `${size.toFixed(size >= 10 ? 0 : 1)} ${units[unitIndex]}`; }
function formatDate(value) { const date = new Date(value); return Number.isNaN(date.valueOf()) ? value : date.toLocaleString(); }
function escapeHtml(value) { return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;"); }
function escapeHtmlAttribute(value) { return escapeHtml(value); }
