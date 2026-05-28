(function () {
    var s = document.createElement('script');
    s.src = '/_framework/blazor.web.js';
    s.onload = function () {
        Blazor.start({
            reconnectionOptions: {
                maxRetries: 10,
                retryIntervalMilliseconds: function (previousAttempts, maxRetries) {
                    return previousAttempts >= maxRetries ? null : previousAttempts * 1000 + 1000;
                }
            }
        });
    };
    document.head.appendChild(s);
})();
