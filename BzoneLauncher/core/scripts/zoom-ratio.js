/*zoomRatio = 1.0;
$(document).ready(function() {
	var body = $('body');
	var minW = body.data('basewidth');
	var minG = body.data('baseheight');
	if(typeof(minW) != "number") minW = 640;
	if(typeof(minG) != "number") minW = 480;
	function CheckSizeZoom() {
		if(($(window).width() * (minG/minW)) > $(window).height()) {
			var zoomLev = $(window).height() / minG;
		} else {
			var zoomLev = $(window).width() / minW;
		}
		if (typeof (document.body.style.zoom) != "undefined") {
			$(document.body).css('zoom', zoomLev);
			zoomRatio = zoomLev;
		}
	}
	CheckSizeZoom();
	$(window).resize(CheckSizeZoom)
});*/