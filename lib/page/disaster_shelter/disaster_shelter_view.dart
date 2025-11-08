import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:get/get.dart';
import 'package:town_pass/gen/assets.gen.dart';
import 'package:url_launcher/url_launcher_string.dart';
import 'dart:async';
import 'dart:convert';
import 'dart:ui' as ui;
import 'dart:typed_data';
import 'package:flutter/services.dart' show rootBundle;
import 'package:town_pass/page/disaster_shelter/event_list_view.dart';
import 'package:town_pass/page/disaster_shelter/shelter_list_view.dart';
import 'package:town_pass/page/disaster_shelter/upload_event_view.dart';
import 'package:town_pass/service/notification_service.dart';
import 'package:town_pass/page/disaster_shelter/test_notification_view.dart';
import 'package:town_pass/util/tp_app_bar.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_text.dart';

class DisasterShelterView extends StatefulWidget {
  const DisasterShelterView({super.key});

  @override
  State<DisasterShelterView> createState() => _DisasterShelterViewState();
}

class _DisasterShelterViewState extends State<DisasterShelterView> {
  static const LatLng _taipei101 = LatLng(25.033964, 121.564468);
  // Default "current" coordinate requested by user
  static const LatLng _mockCurrent = LatLng(25.0217, 121.5351);
  // Taipei City Hall (台北市政府) - destination for directions
  static const LatLng _taipeiCityHall = LatLng(25.0375, 121.5637);

  late final CameraPosition _initialCameraPosition;
  final Set<Marker> _markers = {};
  GoogleMapController? _mapController;

  final NotificationService _notificationService =
      Get.find<NotificationService>();

  List<Shelter> _allShelters = [];
  Timer? _cameraIdleTimer;
  final Map<int, BitmapDescriptor> _clusterIconCache = {};
  int _selectedDisasters = 0; // bitmask of selected disaster types
  int?
      _capacityFilter; // null=any, 1=small(<100),2=medium(100-1000),3=large(>1000)
  int _visibleShelterCount = 0;
  // (no stored currentLatLng needed — markers contain current position)

  @override
  void initState() {
    super.initState();
    // Center initial camera on the mocked "current" location per request
    _initialCameraPosition = const CameraPosition(
      target: _mockCurrent,
      zoom: 14.5,
    );

    _markers.add(
      const Marker(
        markerId: MarkerId('taipei101'),
        position: _taipei101,
        infoWindow: InfoWindow(title: 'Taipei 101'),
      ),
    );
    // Add a current location marker at the requested coordinates 台大體育館
    _markers.add(
      Marker(
        markerId: const MarkerId('current_location'),
        position: _mockCurrent,
        infoWindow: const InfoWindow(title: '您現在的位置'),
        icon: BitmapDescriptor.defaultMarkerWithHue(BitmapDescriptor.hueAzure),
      ),
    );

    // load shelter list from bundled JSON
    _loadShelters();
    ever(_notificationService.isDisasterMode, _handleDisasterModeChange);
  }

  void _handleDisasterModeChange(bool isDisasterMode) {
    if (isDisasterMode) {
      final disasterType = _notificationService.currentDisasterType.value;
      print('進入災害模式，災害類型：$disasterType');
    }
  }

  @override
  void dispose() {
    // cancel any pending camera idle timer to avoid callbacks after dispose
    _cameraIdleTimer?.cancel();
    _cameraIdleTimer = null;
    _mapController?.dispose();
    _mapController = null;
    super.dispose();
  }

  Future<void> _openMapsDirections() async {
    // Use mocked current location as origin and Taipei City Hall as destination
    final origin = '${_mockCurrent.latitude},${_mockCurrent.longitude}';
    final destination =
        '${_taipeiCityHall.latitude},${_taipeiCityHall.longitude}';
    final url =
        'https://www.google.com/maps/dir/?api=1&origin=$origin&destination=$destination&travelmode=walking';

    try {
      await launchUrlString(url);
    } catch (e) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('無法開啟地圖')));
    }
  }

  Future<void> _goToCurrentLocation() async {
    // Use the mocked current location (_mockCurrent) as requested
    final LatLng latlng = _mockCurrent;

    // add or update current location marker
    setState(() {
      _markers
          .removeWhere((m) => m.markerId == const MarkerId('current_location'));
      _markers.add(
        Marker(
          markerId: const MarkerId('current_location'),
          position: latlng,
          infoWindow: const InfoWindow(title: '您現在的位置'),
          icon:
              BitmapDescriptor.defaultMarkerWithHue(BitmapDescriptor.hueAzure),
        ),
      );
    });

    // animate camera to the mocked current location
    final CameraUpdate cu = CameraUpdate.newCameraPosition(
        CameraPosition(target: latlng, zoom: 16));
    try {
      await _mapController?.animateCamera(cu);
    } catch (_) {}
  }

  // Load shelters from bundled JSON asset and parse into models
  Future<void> _loadShelters() async {
    List<dynamic>? rawList;
    try {
      // try the common asset names (project contains shelter_list_small.json)
      String jsonStr;
      try {
        jsonStr = await rootBundle
            .loadString('assets/mock_data/shelter.json');
      } catch (_) {
        jsonStr = await rootBundle
            .loadString('assets/mock_data/shelter_small.json');
      }

      final decoded = json.decode(jsonStr);
      if (decoded is List) {
        rawList = decoded;
      } else if (decoded is Map && decoded['data'] is List) {
        rawList = decoded['data'] as List<dynamic>;
      }

      if (rawList != null) {
        _allShelters = rawList
            .map((e) {
              try {
                return Shelter.fromJson(e as Map<String, dynamic>);
              } catch (err) {
                // ignore parse errors for a single record
                return Shelter.empty();
              }
            })
            .where((s) => s.isValid)
            .toList();

        // debug: print parsed coordinates (first 20) before showing on map
        debugPrint('[_loadShelters] parsed ${_allShelters.length} shelters');
        for (var i = 0; i < _allShelters.length && i < 20; i++) {
          final s = _allShelters[i];
          debugPrint(
              '[shelter $i] ${s.name} -> ${s.latitude}, ${s.longitude} (supported: ${s.supportedDisasters})');
        }
      } else {
        debugPrint('[\u203A_loadShelters] no shelter list found in asset');
      }

      // After loading, update visible markers if controller ready
      _updateVisibleMarkers();
    } catch (e) {
      // non-fatal — keep running with whatever markers we have
      debugPrint('[\u203A_loadShelters] Failed to load shelters: $e');
    }
  }

  // Update markers to include only shelters inside the current visible map bounds
  Future<void> _updateVisibleMarkers() async {
    if (_mapController == null) return;

    try {
      // Diagnostic: counts before/after filtering
      final totalShelters = _allShelters.length;
      final passFilter = _allShelters.where(_matchesFilters).length;

      // collect visible shelters — no longer restricted by map bounds.
      // This ensures filters operate across the entire dataset.
      final visible = _allShelters.where(_matchesFilters).toList();

      // update visible count for UI
      _visibleShelterCount = visible.length;

      // print diagnostics so we can inspect counts
      debugPrint(
          '[diag] total=$totalShelters passFilter=$passFilter visible=${_visibleShelterCount}');
      if (visible.isNotEmpty) {
        for (var i = 0; i < visible.length && i < 10; i++) {
          final s = visible[i];
          debugPrint(
              '[diag-visible $i] ${s.name} @ ${s.latitude},${s.longitude} capacity=${s.capacity} supported=${s.supportedDisasters}');
        }
      } else {
        debugPrint(
            '[diag] no visible shelters after applying filters and bounds');
      }

      // grid-based clustering using screen coordinates
      const int gridSize = 120; // pixels per cluster cell
      final Map<String, List<Shelter>> grid = {};

      for (final s in visible) {
        final latlng = LatLng(s.latitude, s.longitude);
        try {
          final screen = await _mapController!.getScreenCoordinate(latlng);
          final gx = (screen.x / gridSize).floor();
          final gy = (screen.y / gridSize).floor();
          final key = '$gx:$gy';
          grid.putIfAbsent(key, () => []).add(s);
        } catch (e) {
          // skip those we cannot project
        }
      }

      // prepare new marker set: keep non-shelter markers (current_location, taipei101)
      final baseMarkers = _markers
          .where((m) => !(m.markerId.value.startsWith('shelter_') ||
              m.markerId.value.startsWith('cluster_')))
          .toSet();

      final List<Marker> newShelterMarkers = [];
      for (final entry in grid.entries) {
        final group = entry.value;
        if (group.length == 1) {
          final s = group.first;
          final latlng = LatLng(s.latitude, s.longitude);
          final snippetParts = <String>[];
          if (s.address.isNotEmpty) snippetParts.add(s.address);
          if (s.capacity != null) snippetParts.add('容量: ${s.capacity}');
          final markerId = s.id.isNotEmpty
              ? 'shelter_${s.id}'
              : 'shelter_single_${entry.key}';
          debugPrint(
              '[addMarker] $markerId -> ${latlng.latitude}, ${latlng.longitude}');
          newShelterMarkers.add(
            Marker(
              markerId: MarkerId(markerId),
              position: latlng,
              infoWindow:
                  InfoWindow(title: s.name, snippet: snippetParts.join(' • ')),
              icon: BitmapDescriptor.defaultMarkerWithHue(
                  BitmapDescriptor.hueRed),
              zIndex: 1.0,
              onTap: () => _showShelterActions(s),
            ),
          );
        } else {
          // cluster
          // compute centroid
          double sumLat = 0, sumLng = 0;
          for (final s in group) {
            sumLat += s.latitude;
            sumLng += s.longitude;
          }
          final avgLat = sumLat / group.length;
          final avgLng = sumLng / group.length;
          final count = group.length;
          final clusterId = 'cluster_${entry.key}_$count';

          // create or reuse cluster icon
          BitmapDescriptor icon;
          if (_clusterIconCache.containsKey(count)) {
            icon = _clusterIconCache[count]!;
          } else {
            icon = await _createClusterBitmap(count);
            _clusterIconCache[count] = icon;
          }

          debugPrint(
              '[addCluster] $clusterId -> $count items at ${avgLat}, ${avgLng}');
          newShelterMarkers.add(
            Marker(
              markerId: MarkerId(clusterId),
              position: LatLng(avgLat, avgLng),
              infoWindow: InfoWindow(title: '$count 個避難所'),
              icon: icon,
              zIndex: 2.0,
            ),
          );
        }
      }

      setState(() {
        _markers
          ..clear()
          ..addAll(baseMarkers)
          ..addAll(newShelterMarkers);
      });
    } catch (e) {
      // ignore errors from getVisibleRegion or marker updates
    }
  }

  // (old bounds-check helper removed — markers are now filtered across all shelters)

  // show bottom sheet with shelter details and '前往' action
  void _showShelterActions(Shelter s) {
    showModalBottomSheet<void>(
      context: context,
      builder: (ctx) {
        return Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              TPText(s.name,
                  style: TPTextStyles.h3SemiBold, color: TPColors.grayscale900),
              const SizedBox(height: 8),
              if (s.address.isNotEmpty)
                TPText(s.address,
                    style: TPTextStyles.caption, color: TPColors.grayscale700),
              const SizedBox(height: 8),
              Row(
                children: [
                  if (s.capacity != null)
                    TPText('容量: ${s.capacity}',
                        style: TPTextStyles.caption,
                        color: TPColors.grayscale600),
                  const Spacer(),
                  TPText(s.type,
                      style: TPTextStyles.caption,
                      color: TPColors.grayscale600),
                ],
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton(
                      onPressed: () {
                        Navigator.of(ctx).pop();
                        _openMapsDirectionsTo(LatLng(s.latitude, s.longitude));
                      },
                      child: const TPText('前往',
                          style: TPTextStyles.bodySemiBold,
                          color: TPColors.white),
                      style: ElevatedButton.styleFrom(
                          backgroundColor: TPColors.primary500),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
            ],
          ),
        );
      },
    );
  }

  Future<void> _openMapsDirectionsTo(LatLng destination) async {
    final origin = '${_mockCurrent.latitude},${_mockCurrent.longitude}';
    final dest = '${destination.latitude},${destination.longitude}';
    final url =
        'https://www.google.com/maps/dir/?api=1&origin=$origin&destination=$dest&travelmode=walking';
    try {
      await launchUrlString(url);
    } catch (e) {
      ScaffoldMessenger.of(context)
          .showSnackBar(const SnackBar(content: Text('無法開啟地圖')));
    }
  }

  // create a circular cluster bitmap with the count number (cached by count)
  Future<BitmapDescriptor> _createClusterBitmap(int count) async {
    const int size = 120;
    final ui.PictureRecorder recorder = ui.PictureRecorder();
    final Canvas canvas = Canvas(recorder);

    final Paint paint = Paint()..color = Colors.red;
    final double radius = size / 2.0;
    canvas.drawCircle(Offset(radius, radius), radius, paint);

    final textPainter = TextPainter(textDirection: TextDirection.ltr);
    final textStyle = TextStyle(
        color: Colors.white, fontSize: 40, fontWeight: FontWeight.bold);
    textPainter.text = TextSpan(text: count.toString(), style: textStyle);
    textPainter.layout();
    textPainter.paint(
        canvas,
        Offset(
            (size - textPainter.width) / 2, (size - textPainter.height) / 2));

    final ui.Image image = await recorder.endRecording().toImage(size, size);
    final ByteData? bytes =
        await image.toByteData(format: ui.ImageByteFormat.png);
    return BitmapDescriptor.fromBytes(bytes!.buffer.asUint8List());
  }

  bool _matchesFilters(Shelter s) {
    // disaster filter: if none selected -> pass
    if (_selectedDisasters != 0) {
      if ((s.supportedDisasters & _selectedDisasters) == 0) return false;
    }

    // capacity filter
    if (_capacityFilter != null) {
      final c = s.capacity ?? 0;
      if (_capacityFilter == 1 && c >= 100) return false;
      if (_capacityFilter == 2 && (c < 100 || c > 1000)) return false;
      if (_capacityFilter == 3 && c <= 1000) return false;
    }

    return true;
  }

  // return chips representing selected filters (displayed under filter button)
  List<Widget> _buildSelectedFilterChips() {
    final List<Widget> chips = [];
    // disaster chips
    if (_selectedDisasters != 0) {
      if ((_selectedDisasters & 1) != 0) chips.add(_smallChip('淹水'));
      if ((_selectedDisasters & 2) != 0) chips.add(_smallChip('地震'));
      if ((_selectedDisasters & 4) != 0) chips.add(_smallChip('土石流'));
      if ((_selectedDisasters & 8) != 0) chips.add(_smallChip('海嘯'));
      if ((_selectedDisasters & 16) != 0) chips.add(_smallChip('防空'));
    }

    // capacity
    if (_capacityFilter != null) {
      if (_capacityFilter == 1) chips.add(_smallChip('小型 (<100)'));
      if (_capacityFilter == 2) chips.add(_smallChip('中型 (100-1000)'));
      if (_capacityFilter == 3) chips.add(_smallChip('大型 (>1000)'));
    }

    if (chips.isEmpty)
      return [
        TPText('未選擇篩選',
            style: TPTextStyles.caption, color: TPColors.grayscale600)
      ];
    return chips;
  }

  Widget _smallChip(String label) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
          color: TPColors.primary50,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: TPColors.primary100)),
      child: TPText(label,
          style: TPTextStyles.caption, color: TPColors.primary700),
    );
  }

  void _openFilterSheet() {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) {
        // local mutable state for checkboxes inside the sheet
        int tempDisasters = _selectedDisasters;
        int? tempCapacity = _capacityFilter;
        return StatefulBuilder(builder: (c, setS) {
          return Padding(
            padding:
                MediaQuery.of(ctx).viewInsets.add(const EdgeInsets.all(16)),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                TPText('篩選避難所',
                    style: TPTextStyles.h3SemiBold,
                    color: TPColors.grayscale900),
                const SizedBox(height: 12),
                TPText('災難類型',
                    style: TPTextStyles.caption, color: TPColors.grayscale700),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  children: [
                    CheckboxListTile(
                        value: (tempDisasters & 1) != 0,
                        onChanged: (v) => setS(() => tempDisasters =
                            v! ? (tempDisasters | 1) : (tempDisasters & ~1)),
                        title: const TPText('淹水')),
                    CheckboxListTile(
                        value: (tempDisasters & 2) != 0,
                        onChanged: (v) => setS(() => tempDisasters =
                            v! ? (tempDisasters | 2) : (tempDisasters & ~2)),
                        title: const TPText('地震')),
                    CheckboxListTile(
                        value: (tempDisasters & 4) != 0,
                        onChanged: (v) => setS(() => tempDisasters =
                            v! ? (tempDisasters | 4) : (tempDisasters & ~4)),
                        title: const TPText('土石流')),
                    CheckboxListTile(
                        value: (tempDisasters & 8) != 0,
                        onChanged: (v) => setS(() => tempDisasters =
                            v! ? (tempDisasters | 8) : (tempDisasters & ~8)),
                        title: const TPText('海嘯')),
                    CheckboxListTile(
                        value: (tempDisasters & 16) != 0,
                        onChanged: (v) => setS(() => tempDisasters =
                            v! ? (tempDisasters | 16) : (tempDisasters & ~16)),
                        title: const TPText('防空')),
                  ],
                ),
                const SizedBox(height: 8),
                TPText('容量',
                    style: TPTextStyles.caption, color: TPColors.grayscale700),
                RadioListTile<int?>(
                    value: null,
                    groupValue: tempCapacity,
                    title: const TPText('不限'),
                    onChanged: (v) => setS(() => tempCapacity = v)),
                RadioListTile<int?>(
                    value: 1,
                    groupValue: tempCapacity,
                    title: const TPText('小型 (<100)'),
                    onChanged: (v) => setS(() => tempCapacity = v)),
                RadioListTile<int?>(
                    value: 2,
                    groupValue: tempCapacity,
                    title: const TPText('中型 (100-1000)'),
                    onChanged: (v) => setS(() => tempCapacity = v)),
                RadioListTile<int?>(
                    value: 3,
                    groupValue: tempCapacity,
                    title: const TPText('大型 (>1000)'),
                    onChanged: (v) => setS(() => tempCapacity = v)),
                const SizedBox(height: 12),
                Row(
                  children: [
                    TextButton(
                        onPressed: () => Navigator.of(ctx).pop(),
                        child: const TPText('取消')),
                    const Spacer(),
                    TextButton(
                        onPressed: () {
                          setState(() {
                            _selectedDisasters = tempDisasters;
                            _capacityFilter = tempCapacity;
                          });
                          _updateVisibleMarkers();
                          Navigator.of(ctx).pop();
                        },
                        child: const TPText('套用',
                            style: TPTextStyles.bodySemiBold,
                            color: TPColors.primary500)),
                  ],
                ),
              ],
            ),
          );
        });
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: TPColors.white,
      appBar: TPAppBar(title: '避難導航', actions: [
        // 測試通知按鈕（開發階段使用）
        IconButton(
          icon: const Icon(
            Icons.notifications_active,
            size: 36,
          ),
          onPressed: () => Get.to(() => const TestNotificationView()),
        ),
        Obx(() {
          if (_notificationService.isDisasterMode.value) {
            return Container(
              margin: const EdgeInsets.only(right: 16),
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                color: TPColors.red500,
                borderRadius: BorderRadius.circular(20),
              ),
              child: const Row(
                children: [
                  Icon(Icons.warning_amber_rounded,
                      color: Colors.white, size: 16),
                  SizedBox(width: 4),
                  TPText(
                    '災害模式',
                    style: TPTextStyles.caption,
                    color: Colors.white,
                  ),
                ],
              ),
            );
          }
          return const SizedBox.shrink();
        }),
      ]),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Nearest shelter info above the map (stylish card)
            Container(
              decoration: BoxDecoration(
                color: TPColors.white,
                borderRadius: BorderRadius.circular(12),
                boxShadow: [
                  BoxShadow(
                      color: Colors.black.withOpacity(0.05),
                      blurRadius: 8,
                      offset: const Offset(0, 4)),
                ],
              ),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        TPText('您目前最近的避難所：台北市政府',
                            style: TPTextStyles.h3SemiBold,
                            color: TPColors.grayscale900),
                        const SizedBox(height: 6),
                        Row(
                          children: [
                            const Icon(Icons.place,
                                size: 16, color: TPColors.grayscale600),
                            const SizedBox(width: 6),
                            TPText(
                                '您目前的經緯度：${_mockCurrent.latitude.toStringAsFixed(6)}, ${_mockCurrent.longitude.toStringAsFixed(6)}',
                                style: TPTextStyles.caption,
                                color: TPColors.grayscale600),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton.icon(
                    onPressed: _openMapsDirections,
                    icon: const Icon(Icons.directions_walk),
                    label: const TPText('前往',
                        style: TPTextStyles.bodySemiBold,
                        color: TPColors.white),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: TPColors.primary500,
                      padding: const EdgeInsets.symmetric(
                          horizontal: 14, vertical: 12),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10)),
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 12),
            // Compact filter row: open dropdown sheet and show selected tags (two rows)
            Row(
              children: [
                ElevatedButton.icon(
                  onPressed: _openFilterSheet,
                  icon: const Icon(Icons.filter_list),
                  label: const TPText('篩選', style: TPTextStyles.bodySemiBold),
                  style: ElevatedButton.styleFrom(
                      backgroundColor: TPColors.primary500),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: _buildSelectedFilterChips(),
                  ),
                ),
                const SizedBox(width: 12),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    TPText('符合: $_visibleShelterCount 筆',
                        style: TPTextStyles.caption,
                        color: TPColors.grayscale700),
                    TextButton(
                      onPressed: () {
                        setState(() {
                          _selectedDisasters = 0;
                          _capacityFilter = null;
                        });
                        _updateVisibleMarkers();
                      },
                      child: const TPText('清除',
                          style: TPTextStyles.caption,
                          color: TPColors.primary500),
                    ),
                  ],
                ),
              ],
            ),
            // Map area (responsive height, allows zooming). Not strictly 1:1 — nicer layout.
            SizedBox(
              height: MediaQuery.of(context).size.width *
                  1.2, // 60% of screen width
              child: Stack(
                children: [
                  // map with rounded corners and subtle shadow
                  Container(
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(12),
                      boxShadow: [
                        BoxShadow(
                            color: Colors.black.withOpacity(0.06),
                            blurRadius: 10,
                            offset: const Offset(0, 6))
                      ],
                    ),
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(12),
                      child: GoogleMap(
                        initialCameraPosition: _initialCameraPosition,
                        onMapCreated: (controller) {
                          _mapController = controller;
                          // update visible markers once the map is ready
                          _updateVisibleMarkers();
                        },
                        onCameraMove: (pos) {
                          // cancel any pending idle timers while the camera is moving
                          _cameraIdleTimer?.cancel();
                        },
                        onCameraIdle: () {
                          // small debounce: wait briefly before updating markers
                          _cameraIdleTimer?.cancel();
                          _cameraIdleTimer =
                              Timer(const Duration(milliseconds: 300), () {
                            _updateVisibleMarkers();
                          });
                        },
                        markers: _markers,
                        myLocationEnabled: true,
                        myLocationButtonEnabled: true,
                        zoomControlsEnabled: false,
                        zoomGesturesEnabled: true,
                        scrollGesturesEnabled: true,
                        tiltGesturesEnabled: false,
                        rotateGesturesEnabled: false,
                      ),
                    ),
                  ),

                  // Camera button (通報災情) and locate button inside the map, top-right
                  Positioned(
                    top: 12,
                    right: 12,
                    child: Column(
                      children: [
                        // camera
                        GestureDetector(
                          onTap: () => Get.to(() => const UploadEventView()),
                          child: Container(
                            width: 52,
                            height: 52,
                            decoration: BoxDecoration(
                              color: Colors.white,
                              shape: BoxShape.circle,
                              boxShadow: [
                                BoxShadow(
                                    color: Colors.black.withOpacity(0.12),
                                    blurRadius: 8,
                                    offset: const Offset(0, 4))
                              ],
                            ),
                            child: Center(
                              child: Icon(Icons.camera_alt,
                                  color: TPColors.primary500, size: 24),
                            ),
                          ),
                        ),
                        const SizedBox(height: 10),
                        // locate (go to mocked current position)
                        GestureDetector(
                          onTap: _goToCurrentLocation,
                          child: Container(
                            width: 44,
                            height: 44,
                            decoration: BoxDecoration(
                              color: Colors.white,
                              shape: BoxShape.circle,
                              boxShadow: [
                                BoxShadow(
                                    color: Colors.black.withOpacity(0.08),
                                    blurRadius: 6,
                                    offset: const Offset(0, 3))
                              ],
                            ),
                            child: Center(
                              child: Icon(Icons.my_location,
                                  color: TPColors.primary500, size: 20),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            const SizedBox(height: 16),

            // Two side-by-side buttons below the map (primary + outline)
            Row(
              children: [
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: () => Get.to(() => const EventListView()),
                    icon: const Icon(Icons.event_available),
                    label: const TPText('災害事件列表',
                        style: TPTextStyles.bodySemiBold,
                        color: TPColors.white),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: TPColors.primary500,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10)),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () => Get.to(() => const ShelterListView()),
                    icon: const Icon(Icons.home_outlined,
                        color: TPColors.primary500),
                    label: const TPText('避難收容處所',
                        style: TPTextStyles.bodySemiBold,
                        color: TPColors.primary500),
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      side: const BorderSide(color: TPColors.primary500),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10)),
                      backgroundColor: TPColors.white,
                    ),
                  ),
                ),
              ],
            ),

            const SizedBox(height: 16),

            // Optional: keep previous feature cards below if you want
            // _buildFeatureCard(...)
          ],
        ),
      ),
    );
  }

  // removed unused _buildFeatureCard helper
}

// Simple Shelter model for parsing bundled JSON
class Shelter {
  final String id;
  final String type;
  final String name;
  final int? capacity;
  final int supportedDisasters;
  final bool accessibility;
  final String address;
  final double latitude;
  final double longitude;
  final String telephone;
  final double? sizeInSquareMeters;

  Shelter({
    required this.id,
    required this.type,
    required this.name,
    this.capacity,
    required this.supportedDisasters,
    required this.accessibility,
    required this.address,
    required this.latitude,
    required this.longitude,
    required this.telephone,
    this.sizeInSquareMeters,
  });

  bool get isValid => name.isNotEmpty && latitude != 0.0 && longitude != 0.0;

  // bitflag helpers
  bool get isFlooding => (supportedDisasters & 1) != 0;
  bool get isEarthquake => (supportedDisasters & 2) != 0;
  bool get isLandslide => (supportedDisasters & 4) != 0;
  bool get isTsunami => (supportedDisasters & 8) != 0;
  bool get isAirRaid => (supportedDisasters & 16) != 0;

  factory Shelter.fromJson(Map<String, dynamic> json) {
    int parseInt(dynamic v) {
      if (v is int) return v;
      if (v is String) return int.tryParse(v) ?? 0;
      if (v is double) return v.toInt();
      return 0;
    }

    double parseDouble(dynamic v) {
      if (v is double) return v;
      if (v is int) return v.toDouble();
      if (v is String) return double.tryParse(v) ?? 0.0;
      return 0.0;
    }

    bool parseBool(dynamic v) {
      if (v is bool) return v;
      if (v is String) return v.toLowerCase() == 'true' || v == '1';
      if (v is int) return v != 0;
      return false;
    }

    final lat = parseDouble(json['latitude'] ??
        json['lat'] ??
        json['y'] ??
        json['latitude_deg'] ??
        0);
    final lng = parseDouble(json['longitude'] ??
        json['lng'] ??
        json['x'] ??
        json['longitude_deg'] ??
        0);

    final supported = parseInt(json['supportedDisasters'] ??
        json['supported_disasters'] ??
        json['supported'] ??
        0);

    // accessibility key may be misspelled in some data sources as 'accesibility'
    final accessVal = json.containsKey('accessibility')
        ? json['accessibility']
        : json['accesibility'];

    return Shelter(
      id: json['id']?.toString() ?? '',
      type: json['type']?.toString() ?? '',
      name: json['name']?.toString() ?? json['place']?.toString() ?? '避難所',
      capacity:
          json.containsKey('capacity') ? parseInt(json['capacity']) : null,
      supportedDisasters: supported,
      accessibility: parseBool(accessVal),
      address: json['address']?.toString() ?? '',
      latitude: lat,
      longitude: lng,
      telephone: json['telephone']?.toString() ?? json['tel']?.toString() ?? '',
      sizeInSquareMeters: json.containsKey('sizeInSquareMeters')
          ? parseDouble(json['sizeInSquareMeters'])
          : (json.containsKey('size') ? parseDouble(json['size']) : null),
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'type': type,
        'name': name,
        'capacity': capacity,
        'supportedDisasters': supportedDisasters,
        'accessibility': accessibility,
        'address': address,
        'latitude': latitude,
        'longitude': longitude,
        'telephone': telephone,
        'sizeInSquareMeters': sizeInSquareMeters,
      };

  Shelter.empty()
      : id = '',
        type = '',
        name = '',
        capacity = null,
        supportedDisasters = 0,
        accessibility = false,
        address = '',
        latitude = 0.0,
        longitude = 0.0,
        telephone = '',
        sizeInSquareMeters = null;

  Shelter copyWith({
    String? id,
    String? type,
    String? name,
    int? capacity,
    int? supportedDisasters,
    bool? accessibility,
    String? address,
    double? latitude,
    double? longitude,
    String? telephone,
    double? sizeInSquareMeters,
  }) {
    return Shelter(
      id: id ?? this.id,
      type: type ?? this.type,
      name: name ?? this.name,
      capacity: capacity ?? this.capacity,
      supportedDisasters: supportedDisasters ?? this.supportedDisasters,
      accessibility: accessibility ?? this.accessibility,
      address: address ?? this.address,
      latitude: latitude ?? this.latitude,
      longitude: longitude ?? this.longitude,
      telephone: telephone ?? this.telephone,
      sizeInSquareMeters: sizeInSquareMeters ?? this.sizeInSquareMeters,
    );
  }
}
