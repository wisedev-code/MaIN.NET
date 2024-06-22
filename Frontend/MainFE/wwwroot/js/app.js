window.applyCodeStyles = function() {
    document.querySelectorAll('code').forEach(function(el) {
        el.style.backgroundColor = '#f5f5f5';
        el.style.borderRadius = '3px';
        el.style.padding = '2px 4px';
        el.style.fontFamily = 'Consolas, "Courier New", monospace';
        el.style.color = '#d63384';
    });
};