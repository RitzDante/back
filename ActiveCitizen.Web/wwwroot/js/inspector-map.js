document.addEventListener('DOMContentLoaded', function () {
    const mapElement = document.getElementById('map');

    if (!mapElement) {
        return;
    }

    if (typeof ymaps === 'undefined') {
        console.error('Yandex Maps API не загружен.');
        return;
    }

    if (!window.claimsData || !Array.isArray(window.claimsData)) {
        console.warn('window.claimsData не найден или не является массивом.');
        return;
    }

    const statusFilter = document.getElementById('statusFilter');
    const violationTypeFilter = document.getElementById('violationTypeFilter');
    const tableRows = document.querySelectorAll('#claimsTableBody tr');
    const noFilteredClaimsMessage = document.getElementById('noFilteredClaimsMessage');

    let map;
    let placemarks = [];

    ymaps.ready(initMap);

    function initMap() {
        map = new ymaps.Map('map', {
            center: [59.9343, 30.3351],
            zoom: 11,
            controls: ['zoomControl', 'fullscreenControl']
        });

        createPlacemarks();
        applyFilters();

        if (statusFilter) {
            statusFilter.addEventListener('change', applyFilters);
        }

        if (violationTypeFilter) {
            violationTypeFilter.addEventListener('change', applyFilters);
        }
    }

    function createPlacemarks() {
        placemarks = [];

        window.claimsData.forEach(function (claim) {
            const id = getValue(claim, 'id', 'Id');
            const latitude = toNumber(getValue(claim, 'latitude', 'Latitude'));
            const longitude = toNumber(getValue(claim, 'longitude', 'Longitude'));

            const statusName = getValue(claim, 'statusName', 'StatusName') || '';
            const violationTypeName = getValue(claim, 'violationTypeName', 'ViolationTypeName') || '';
            const address = getValue(claim, 'address', 'Address') || '';

            if (latitude === null || longitude === null) {
                console.warn('Заявка без координат пропущена:', claim);
                return;
            }

            const normalizedStatus = normalizeStatus(statusName);
            const normalizedViolationType = normalize(violationTypeName);

            const placemark = new ymaps.Placemark(
                [latitude, longitude],
                {
                    balloonContent:
                        '<strong>Заявка №' + escapeHtml(id) + '</strong><br>' +
                        '<strong>' + escapeHtml(violationTypeName) + '</strong><br>' +
                        escapeHtml(address) + '<br>' +
                        'Статус: ' + escapeHtml(statusName) + '<br><br>' +
                        '<a href="/Inspector/Details/' + encodeURIComponent(id) + '">Подробнее</a>',
                    hintContent: 'Заявка №' + id
                },
                {
                    preset: getMarkerPreset(normalizedStatus)
                }
            );

            placemark.properties.set('claimStatus', normalizedStatus);
            placemark.properties.set('claimViolationType', normalizedViolationType);

            placemarks.push(placemark);
            map.geoObjects.add(placemark);
        });
    }

    function applyFilters() {
        const selectedStatus = normalizeStatus(statusFilter ? statusFilter.value : '');
        const selectedViolationType = normalize(violationTypeFilter ? violationTypeFilter.value : '');

        filterTable(selectedStatus, selectedViolationType);
        filterMap(selectedStatus, selectedViolationType);
    }

    function filterTable(selectedStatus, selectedViolationType) {
        let visibleRowsCount = 0;

        tableRows.forEach(function (row) {
            const rowStatus = normalizeStatus(row.dataset.status || '');
            const rowViolationType = normalize(row.dataset.violationType || '');

            const statusMatches = selectedStatus === '' || rowStatus === selectedStatus;
            const typeMatches = selectedViolationType === '' || rowViolationType === selectedViolationType;

            const shouldShow = statusMatches && typeMatches;

            row.style.display = shouldShow ? '' : 'none';

            if (shouldShow) {
                visibleRowsCount++;
            }
        });

        if (noFilteredClaimsMessage) {
            noFilteredClaimsMessage.classList.toggle('d-none', visibleRowsCount > 0);
        }
    }

    function filterMap(selectedStatus, selectedViolationType) {
        const visiblePlacemarks = [];

        placemarks.forEach(function (placemark) {
            const markerStatus = normalizeStatus(placemark.properties.get('claimStatus'));
            const markerViolationType = normalize(placemark.properties.get('claimViolationType'));

            const statusMatches = selectedStatus === '' || markerStatus === selectedStatus;
            const typeMatches = selectedViolationType === '' || markerViolationType === selectedViolationType;

            const shouldShow = statusMatches && typeMatches;

            placemark.options.set('visible', shouldShow);

            if (shouldShow) {
                visiblePlacemarks.push(placemark);
            }
        });

        if (visiblePlacemarks.length === 1) {
            const coords = visiblePlacemarks[0].geometry.getCoordinates();

            map.setCenter(coords, 13, {
                duration: 300
            });

            return;
        }

        if (visiblePlacemarks.length > 1) {
            const collection = new ymaps.GeoObjectCollection();

            visiblePlacemarks.forEach(function (placemark) {
                collection.add(placemark);
            });

            const bounds = collection.getBounds();

            if (bounds) {
                map.setBounds(bounds, {
                    checkZoomRange: true,
                    zoomMargin: 40
                });
            }
        }
    }

    function getMarkerPreset(status) {
        switch (status) {
            case 'новая':
                return 'islands#orangeDotIcon';

            case 'в работе':
                return 'islands#blueDotIcon';

            case 'завершена':
                return 'islands#greenDotIcon';

            case 'отклонена':
                return 'islands#redDotIcon';

            default:
                return 'islands#grayDotIcon';
        }
    }

    function normalizeStatus(value) {
        const status = normalize(value);

        if (status === 'новая') return 'новая';
        if (status === 'в работе') return 'в работе';
        if (status === 'завершена') return 'завершена';
        if (status === 'отклонена') return 'отклонена';

        return status;
    }

    function normalize(value) {
        return String(value || '').toLowerCase().trim();
    }

    function getValue(obj, camelCaseName, pascalCaseName) {
        if (!obj) {
            return null;
        }

        if (obj[camelCaseName] !== undefined && obj[camelCaseName] !== null) {
            return obj[camelCaseName];
        }

        if (obj[pascalCaseName] !== undefined && obj[pascalCaseName] !== null) {
            return obj[pascalCaseName];
        }

        return null;
    }

    function toNumber(value) {
        if (value === undefined || value === null || value === '') {
            return null;
        }

        if (typeof value === 'number') {
            return value;
        }

        const normalized = String(value).replace(',', '.');
        const result = Number(normalized);

        return Number.isFinite(result) ? result : null;
    }

    function escapeHtml(value) {
        return String(value || '')
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }
});