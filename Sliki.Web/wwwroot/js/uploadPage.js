const uploaders = new Map();

export function initializeUploader(rootId) {
    const root = document.getElementById(rootId);
    if (!root) {
        return;
    }

    disposeUploader(rootId);

    const config = {
        rootId,
        uploadUrl: root.dataset.uploadUrl,
        maxUploadBytes: Number(root.dataset.maxUploadBytes || "0"),
        acceptedMimeTypes: (root.dataset.acceptedMimeTypes || "").split(",").filter(Boolean)
    };

    const state = {
        root,
        config,
        input: root.querySelector("#browser-upload-input"),
        uploadAllButton: root.querySelector("[data-upload-action='upload-all']"),
        retryFailedButton: root.querySelector("[data-upload-action='retry-failed']"),
        clearSelectionButton: root.querySelector("[data-upload-action='clear-selection']"),
        statusMessage: root.querySelector("[data-upload-status]"),
        selectionMessage: root.querySelector("[data-upload-selection-message]"),
        emptyState: root.querySelector("[data-upload-empty-state]"),
        list: root.querySelector("[data-upload-list]"),
        items: []
    };

    if (!state.input || !state.uploadAllButton || !state.retryFailedButton || !state.clearSelectionButton || !state.statusMessage || !state.selectionMessage || !state.emptyState || !state.list) {
        return;
    }

    state.onChange = () => handleSelection(state);
    state.onUploadAll = () => uploadMatching(state, item => item.state === "pending" || item.state === "failed");
    state.onRetryFailed = () => uploadMatching(state, item => item.state === "failed");
    state.onClear = () => clearSelection(state);

    state.input.addEventListener("change", state.onChange);
    state.uploadAllButton.addEventListener("click", state.onUploadAll);
    state.retryFailedButton.addEventListener("click", state.onRetryFailed);
    state.clearSelectionButton.addEventListener("click", state.onClear);

    uploaders.set(rootId, state);
    render(state);
}

export function disposeUploader(rootId) {
    const state = uploaders.get(rootId);
    if (!state) {
        return;
    }

    state.input?.removeEventListener("change", state.onChange);
    state.uploadAllButton?.removeEventListener("click", state.onUploadAll);
    state.retryFailedButton?.removeEventListener("click", state.onRetryFailed);
    state.clearSelectionButton?.removeEventListener("click", state.onClear);
    revokePreviewUrls(state.items);
    uploaders.delete(rootId);
}

function handleSelection(state) {
    setText(state.statusMessage, "");
    setText(state.selectionMessage, "");

    const files = Array.from(state.input.files || []);
    if (!files.length) {
        render(state);
        return;
    }

    const allowedMimeTypes = new Set(state.config.acceptedMimeTypes || []);
    const newItems = files.map(file => {
        const item = {
            id: crypto.randomUUID(),
            file,
            displayName: file.name,
            browserContentType: file.type || "unknown",
            sizeInBytes: file.size,
            previewUrl: file.type.startsWith("image/") ? URL.createObjectURL(file) : null,
            state: "pending",
            progressPercent: 0,
            statusDetails: "Ready to upload.",
            errorMessage: ""
        };

        if (file.size > state.config.maxUploadBytes) {
            item.state = "failed";
            item.statusDetails = "File rejected before upload.";
            item.errorMessage = `The file exceeds the limit of ${formatFileSize(state.config.maxUploadBytes)}.`;
        } else if (file.type && !allowedMimeTypes.has(file.type)) {
            item.state = "failed";
            item.statusDetails = "File rejected before upload.";
            item.errorMessage = "The selected file type is not allowed.";
        }

        return item;
    });

    revokePreviewUrls(state.items);
    state.items = newItems;
    render(state);
}

async function uploadMatching(state, predicate) {
    const targets = state.items.filter(predicate);
    if (!targets.length) {
        return;
    }

    setText(state.statusMessage, "");
    let uploadedCount = 0;

    for (const item of targets) {
        const uploaded = await uploadItem(state, item);
        if (uploaded) {
            uploadedCount += 1;
        }
        render(state);
    }

    if (uploadedCount > 0) {
        setText(
            state.statusMessage,
            uploadedCount === 1
                ? "1 image uploaded successfully."
                : `${uploadedCount} images uploaded successfully.`);
    }
}

function uploadItem(state, item) {
    return new Promise(resolve => {
        item.state = "uploading";
        item.progressPercent = 0;
        item.statusDetails = "Uploading to Azure Blob Storage...";
        item.errorMessage = "";
        render(state);

        const formData = new FormData();
        formData.append("file", item.file, item.file.name);

        const request = new XMLHttpRequest();
        request.open("POST", state.config.uploadUrl, true);

        request.upload.addEventListener("progress", event => {
            if (!event.lengthComputable) {
                return;
            }

            item.progressPercent = Math.max(1, Math.min(100, Math.round((event.loaded / event.total) * 100)));
            item.statusDetails = "Uploading to Azure Blob Storage...";
            render(state);
        });

        request.addEventListener("load", () => {
            if (request.status >= 200 && request.status < 300) {
                item.state = "success";
                item.progressPercent = 100;
                item.statusDetails = "Uploaded successfully.";
                resolve(true);
                return;
            }

            item.state = "failed";
            item.progressPercent = 0;
            item.statusDetails = "Upload failed.";
            item.errorMessage = parseErrorMessage(request.responseText);
            resolve(false);
        });

        request.addEventListener("error", () => {
            item.state = "failed";
            item.progressPercent = 0;
            item.statusDetails = "Upload failed.";
            item.errorMessage = "The upload request could not reach the server.";
            resolve(false);
        });

        request.send(formData);
    });
}

function clearSelection(state) {
    revokePreviewUrls(state.items);
    state.items = [];
    state.input.value = "";
    setText(state.statusMessage, "");
    setText(state.selectionMessage, "");
    render(state);
}

function revokePreviewUrls(items) {
    for (const item of items) {
        if (item.previewUrl) {
            URL.revokeObjectURL(item.previewUrl);
        }
    }
}

function render(state) {
    state.emptyState.classList.toggle("d-none", state.items.length > 0);
    state.list.classList.toggle("d-none", state.items.length === 0);

    const canUploadAny = state.items.some(item => item.state === "pending" || item.state === "failed");
    const hasFailedItems = state.items.some(item => item.state === "failed");
    const isUploading = state.items.some(item => item.state === "uploading");

    state.uploadAllButton.disabled = !canUploadAny || isUploading;
    state.retryFailedButton.disabled = !hasFailedItems || isUploading;
    state.clearSelectionButton.disabled = state.items.length === 0 || isUploading;

    if (state.items.length === 0) {
        state.list.innerHTML = "";
        return;
    }

    state.list.innerHTML = state.items.map(item => `
        <article class="surface-card upload-item">
            <div class="preview-frame">
                ${item.previewUrl
                    ? `<img src="${item.previewUrl}" alt="" class="preview-image" />`
                    : `<div class="preview-placeholder" aria-hidden="true">IMG</div>`}
            </div>

            <div class="upload-item-main">
                <div class="upload-item-header">
                    <div>
                        <h3>${escapeHtml(item.displayName)}</h3>
                        <p class="subtle-copy">${formatFileSize(item.sizeInBytes)} • ${escapeHtml(item.browserContentType)}</p>
                    </div>
                    <span class="${getStatusBadgeClass(item.state)}">${getStatusLabel(item.state)}</span>
                </div>

                <div class="progress" role="progressbar" aria-label="Upload progress" aria-valuenow="${item.progressPercent}" aria-valuemin="0" aria-valuemax="100">
                    <div class="progress-bar" style="width:${item.progressPercent}%">${item.progressPercent}%</div>
                </div>

                <p class="status-copy">${escapeHtml(item.statusDetails)}</p>
                ${item.errorMessage ? `<p class="text-danger mb-0">${escapeHtml(item.errorMessage)}</p>` : ""}
            </div>

            <div class="upload-item-actions">
                <button type="button" class="btn btn-outline-primary" data-item-action="upload" data-item-id="${item.id}" ${item.state === "uploading" ? "disabled" : ""}>
                    ${item.state === "failed" ? "Retry" : "Upload"}
                </button>
            </div>
        </article>
    `).join("");

    for (const button of state.list.querySelectorAll("[data-item-action='upload']")) {
        button.addEventListener("click", async event => {
            const itemId = event.currentTarget.getAttribute("data-item-id");
            const item = state.items.find(candidate => candidate.id === itemId);
            if (!item || item.state === "uploading") {
                return;
            }

            await uploadItem(state, item);
            render(state);
        }, { once: true });
    }
}

function getStatusBadgeClass(state) {
    switch (state) {
        case "success":
            return "badge text-bg-success";
        case "uploading":
            return "badge text-bg-primary";
        case "failed":
            return "badge text-bg-danger";
        default:
            return "badge text-bg-secondary";
    }
}

function getStatusLabel(state) {
    switch (state) {
        case "success":
            return "Uploaded";
        case "uploading":
            return "Uploading";
        case "failed":
            return "Failed";
        default:
            return "Ready";
    }
}

function parseErrorMessage(responseText) {
    try {
        const parsed = JSON.parse(responseText);
        return parsed.error || "The upload failed.";
    } catch {
        return responseText || "The upload failed.";
    }
}

function setText(element, text) {
    element.textContent = text;
    element.classList.toggle("d-none", !text);
}

function escapeHtml(value) {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;");
}

function formatFileSize(value) {
    if (value < 1024) {
        return `${value} bytes`;
    }

    const units = ["KB", "MB", "GB"];
    let size = value / 1024;
    let unitIndex = 0;

    while (size >= 1024 && unitIndex < units.length - 1) {
        size /= 1024;
        unitIndex += 1;
    }

    return `${size.toFixed(size >= 10 ? 0 : 1)} ${units[unitIndex]}`;
}
