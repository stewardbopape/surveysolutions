const minHeight = 21;
const minHeightPx = minHeight + 'px';

const autosize = app => {
    app.directive('autosize', {
        mounted(el) {
            el.style.overflow = 'hidden';
            el.style.resize = 'none';
            el.style.boxSizing = 'border-box';
            el.rows = 1;

            const calculateHeight = () => {
                el.style.height = 'auto';

                requestAnimationFrame(() => {
                    const newHeight =
                        el.scrollHeight + el.offsetHeight - el.clientHeight;
                    el.style.height =
                        newHeight < minHeight ? minHeightPx : newHeight + 'px';
                });
            };

            el.calculateHeight = calculateHeight;

            calculateHeight();

            el.addEventListener('input', calculateHeight);
        },
        unmounted(el) {
            el.removeEventListener('input', el.calculateHeight);
        }
    });
};

export default autosize;
