﻿function userTimeZone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}

function setTimeZoneCookie(timeZoneId) {
    document.cookie = "TimeZone=" + timeZoneId + "; path=/";
}

function timeZoneCookie() {
    return getCookie("TimeZone");
}

$(() => {
    if (!timeZoneCookie()) {
        setTimeZoneCookie(userTimeZone());
        location.reload();
    }
});

function getCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');

    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];

        while (c.charAt(0) == ' ') {
            c = c.substring(1, c.length);
        }

        if (c.indexOf(nameEQ) == 0) {
            return c.substring(nameEQ.length, c.length);
        }
    }

    return null;
}