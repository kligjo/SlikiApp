window.slikiGallery = {
    _fileRegistry: new Map(),

    downloadFile(url, filename) {
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    },

    registerFiles(inputId, generatePreviews = true) {
        const input = document.getElementById(inputId);
        if (!input?.files) return [];
        const result = [];
        for (const file of input.files) {
            const id = crypto.randomUUID();
            this._fileRegistry.set(id, file);
            result.push({
                id,
                previewUrl: generatePreviews && file.type.startsWith('image/') ? URL.createObjectURL(file) : null,
                name: file.name,
                size: file.size,
                type: file.type || 'application/octet-stream'
            });
        }
        return result;
    },

    uploadFile(fileId, dotnetRef) {
        const file = this._fileRegistry.get(fileId);
        if (!file) {
            dotnetRef.invokeMethodAsync('OnUploadDone', false, '', 'File reference lost — please re-select the file.');
            return;
        }
        const formData = new FormData();
        formData.append('file', file, file.name);
        const xhr = new XMLHttpRequest();
        xhr.open('POST', '/api/images/upload');
        xhr.upload.onprogress = e => {
            if (e.lengthComputable)
                dotnetRef.invokeMethodAsync('OnUploadProgress', Math.round(e.loaded / e.total * 100));
        };
        xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status < 300) {
                dotnetRef.invokeMethodAsync('OnUploadDone', true, xhr.responseText, '');
            } else {
                let error = 'Upload failed.';
                try { error = JSON.parse(xhr.responseText).error || error; } catch {}
                dotnetRef.invokeMethodAsync('OnUploadDone', false, '', error);
            }
        };
        xhr.onerror = () => dotnetRef.invokeMethodAsync('OnUploadDone', false, '', 'Network error.');
        xhr.send(formData);
    },

    // SAS-based direct-to-blob upload:
    // 1. Reads first 32 bytes for server-side magic-byte validation
    // 2. POST /slikar/api/sas → gets a SAS URL
    // 3. PUT directly to Azure Blob Storage (bypasses the app server entirely)
    // 4. POST /slikar/api/complete so the server can enqueue thumbnail generation
    async uploadViaSas(fileId, dotnetRef, sasEndpoint, completeEndpoint) {
        const file = this._fileRegistry.get(fileId);
        if (!file) {
            dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, false, '', 'File reference lost — please re-select.');
            return;
        }
        try {
            // Read first 32 bytes for server-side magic-byte detection
            const headerSlice = file.slice(0, 32);
            const headerBytes = await headerSlice.arrayBuffer();
            const headerBase64 = btoa(String.fromCharCode(...new Uint8Array(headerBytes)));

            // Request a SAS ticket from our server
            const sasForm = new FormData();
            sasForm.append('fileName', file.name);
            sasForm.append('contentType', file.type || 'application/octet-stream');
            sasForm.append('size', file.size);
            sasForm.append('headerBase64', headerBase64);

            const sasResp = await fetch(sasEndpoint, { method: 'POST', body: sasForm });
            if (!sasResp.ok) {
                let error = 'Validation failed.';
                try { error = (await sasResp.json()).error || error; } catch {}
                dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, false, '', error);
                return;
            }
            const ticket = await sasResp.json();

            // PUT directly to Azure Blob Storage using the SAS URL
            const putResp = await fetch(ticket.sasUrl, {
                method: 'PUT',
                headers: {
                    'x-ms-blob-type': 'BlockBlob',
                    'Content-Type': ticket.contentType
                },
                body: file
            });
            if (!putResp.ok) {
                dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, false, '', `Blob upload failed: ${putResp.status}`);
                return;
            }

            // Notify server so it can set blob metadata and enqueue thumbnail generation
            const completeForm = new FormData();
            completeForm.append('blobName', ticket.blobName);
            completeForm.append('fileName', ticket.fileName);
            completeForm.append('contentType', ticket.contentType);
            await fetch(completeEndpoint, { method: 'POST', body: completeForm });

            dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, true, JSON.stringify(ticket), '');
        } catch (err) {
            dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, false, '', err?.message || 'Network error.');
        }
    },

    releaseFiles(fileIds) {
        for (const id of fileIds) this._fileRegistry.delete(id);
    },

    revokePreviewUrls(urls) {
        for (const url of urls) { if (url) URL.revokeObjectURL(url); }
    },

    generateVideoThumbnails() {
        if (!this._thumbObserver) {
            this._thumbObserver = new IntersectionObserver(entries => {
                for (const entry of entries) {
                    if (!entry.isIntersecting) continue;
                    const video = entry.target;
                    if (video.dataset.thumbDone) continue;
                    video.dataset.thumbDone = '1';
                    this._thumbObserver.unobserve(video);
                    this._captureVideoThumb(video);
                }
            }, { rootMargin: '400px' });
        }
        for (const video of document.querySelectorAll('.photo-grid video.photo-thumb')) {
            if (!video.dataset.thumbDone) {
                this._thumbObserver.observe(video);
            }
        }
    },

    _captureVideoThumb(video) {
        const src = video.src;
        if (!src) return;
        const tmp = document.createElement('video');
        tmp.muted = true;
        tmp.playsInline = true;
        tmp.preload = 'metadata';
        tmp.src = src;
        tmp.addEventListener('loadedmetadata', () => {
            tmp.currentTime = tmp.duration > 1 ? 1 : 0.01;
        }, { once: true });
        tmp.addEventListener('seeked', () => {
            try {
                const canvas = document.createElement('canvas');
                canvas.width = tmp.videoWidth || 320;
                canvas.height = tmp.videoHeight || 180;
                canvas.getContext('2d').drawImage(tmp, 0, 0, canvas.width, canvas.height);
                video.poster = canvas.toDataURL('image/jpeg', 0.75);
            } catch (_) {}
            tmp.src = '';
            tmp.load();
        }, { once: true });
        tmp.load();
    },

    initInfiniteScroll(dotnetRef) {
        this.disposeInfiniteScroll();
        const sentinel = document.getElementById('scroll-sentinel');
        if (!sentinel) return;
        this._scrollObserver = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting) {
                dotnetRef.invokeMethodAsync('OnSentinelVisible');
            }
        }, { rootMargin: '400px' });
        this._scrollObserver.observe(sentinel);
    },

    disposeInfiniteScroll() {
        if (this._scrollObserver) {
            this._scrollObserver.disconnect();
            this._scrollObserver = null;
        }
    }
};
