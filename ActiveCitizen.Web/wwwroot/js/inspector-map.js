document.addEventListener('DOMContentLoaded', function () {
    if (typeof window.claimsData === 'undefined') {
        console.warn('claimsData not found. No claims to display.');
        return;
    }

    ymaps.ready(function () {
        var map = new ymaps.Map('map', {
            center: [59.9343, 30.3351],
            zoom: 11,
            controls: ['zoomControl', 'fullscreenControl']
        });

        var claims = window.claimsData; 

        claims.forEach(function (claim) {
            if (claim.latitude && claim.longitude) {
                var placemark = new ymaps.Placemark(
                    [claim.latitude, claim.longitude],
                    {
                        balloonContent: `<b>${claim.violationTypeName}</b><br/>${claim.address}<br/><a href="/Inspector/Details/${claim.id}">Подробнее</a>`,
                        hintContent: claim.violationTypeName,
                        status: claim.statusName.toLowerCase(),
                        violationType: claim.violationTypeName.toLowerCase()
                    },
                    {
                        iconLayout: 'default#image',
                        iconImageHref: 'https://cdn-icons-png.flaticon.com/512/684/684908.png',
                        iconImageSize: [24, 24],
                        iconImageOffset: [-12, -12]
                    }
                );
                map.geoObjects.add(placemark);
            }
        });

        const statusFilter = document.getElementById('statusFilter');
        const violationTypeFilter = document.getElementById('violationTypeFilter');

        function applyFilters() {
            const selectedStatus = statusFilter.value.toLowerCase();
            const selectedViolationType = violationTypeFilter.value.toLowerCase();

            map.geoObjects.each(function (obj) {
                const markerStatus = obj.properties.get('status')?.toLowerCase() || '';
                const markerViolationType = obj.properties.get('violationType')?.toLowerCase() || '';

                const show = (!selectedStatus || markerStatus.includes(selectedStatus)) &&
                    (!selectedViolationType || markerViolationType.includes(selectedViolationType));

                obj.options.set('visible', show);
            });
        }

        statusFilter.addEventListener('change', applyFilters);
        violationTypeFilter.addEventListener('change', applyFilters);
    });
});