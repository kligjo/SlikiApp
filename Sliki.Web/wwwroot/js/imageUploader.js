let previewUrls = [];

export function getImagePreviewUrls(inputId) {
    revokeImagePreviewUrls();

    const input = document.getElementById(inputId);
    if (!input || !input.files) {
        return [];
    }

    previewUrls = Array.from(input.files)
        .filter(file => file.type.startsWith("image/"))
        .map(file => URL.createObjectURL(file));

    return previewUrls;
}

export function revokeImagePreviewUrls() {
    for (const url of previewUrls) {
        URL.revokeObjectURL(url);
    }

    previewUrls = [];
}
