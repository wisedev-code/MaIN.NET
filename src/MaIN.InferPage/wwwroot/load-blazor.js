Blazor.start({
    reconnectionOptions: {
        maxRetries: 10,
        retryIntervalMilliseconds: function (previousAttempts, maxRetries) {
            return previousAttempts >= maxRetries ? null : previousAttempts * 1000 + 1000;
        }
    }
});
