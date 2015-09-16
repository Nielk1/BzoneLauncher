$(document).ready(function () {
    $(document).keydown(function (event) {
        if (event.keyCode == 13 && event.altKey) {
            Launcher.ToggleFullscreen();
            event.preventDefault();
        }
    });
});