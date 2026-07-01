window.slikiGallery = {
    downloadFile(url, filename) {
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
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
