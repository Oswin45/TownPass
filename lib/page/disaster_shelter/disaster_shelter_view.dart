import 'package:flutter/material.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:google_maps_flutter/google_maps_flutter.dart';
import 'package:get/get.dart';
import 'package:geolocator/geolocator.dart';
import 'package:url_launcher/url_launcher_string.dart';
import 'package:town_pass/page/disaster_shelter/event_list_view.dart';
import 'package:town_pass/page/disaster_shelter/shelter_list_view.dart';
import 'package:town_pass/page/disaster_shelter/upload_event_view.dart';
import 'package:town_pass/page/disaster_shelter/shelter_list_view.dart'; 
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
  // Destination: 中央大學 (示範座標)
  static const LatLng _ncu = LatLng(24.9615, 121.1886);

  late final CameraPosition _initialCameraPosition;
  final Set<Marker> _markers = {};
  GoogleMapController? _mapController;

  @override
  void initState() {
    super.initState();
    _initialCameraPosition = const CameraPosition(
      target: _taipei101,
      zoom: 14.5,
    );

    _markers.add(
      const Marker(
        markerId: MarkerId('taipei101'),
        position: _taipei101,
        infoWindow: InfoWindow(title: 'Taipei 101'),
      ),
    );
  }

  @override
  void dispose() {
    _mapController?.dispose();
    super.dispose();
  }

  Future<Position?> _determinePosition() async {
    bool serviceEnabled;
    LocationPermission permission;

    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      // Location services are not enabled
      return null;
    }

    permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        return null;
      }
    }

    if (permission == LocationPermission.deniedForever) {
      return null;
    }

    return await Geolocator.getCurrentPosition(desiredAccuracy: LocationAccuracy.high);
  }

  Future<void> _openMapsDirections() async {
    // Get current position
    final pos = await _determinePosition();
    if (pos == null) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('無法取得目前位置，請確認權限與定位服務是否開啟')));
      return;
    }

    final origin = '${pos.latitude},${pos.longitude}';
    final destination = '${_ncu.latitude},${_ncu.longitude}';
    final url = 'https://www.google.com/maps/dir/?api=1&origin=$origin&destination=$destination&travelmode=walking';

    try {
      await launchUrlString(url);
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('無法開啟地圖')));
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: TPColors.white,
      appBar: const TPAppBar(
        title: '避難導航',
      ),
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
                  BoxShadow(color: Colors.black.withOpacity(0.05), blurRadius: 8, offset: const Offset(0, 4)),
                ],
              ),
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        TPText('您目前最近的避難所：台北市政府', style: TPTextStyles.h3SemiBold, color: TPColors.grayscale900),
                        const SizedBox(height: 6),
                        Row(
                          children: [
                            const Icon(Icons.place, size: 16, color: TPColors.grayscale600),
                            const SizedBox(width: 6),
                            TPText('經緯度：${_ncu.latitude.toStringAsFixed(6)}, ${_ncu.longitude.toStringAsFixed(6)}', style: TPTextStyles.caption, color: TPColors.grayscale600),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 8),
                  ElevatedButton.icon(
                    onPressed: _openMapsDirections,
                    icon: const Icon(Icons.directions_walk),
                    label: const TPText('前往', style: TPTextStyles.bodySemiBold, color: TPColors.white),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: TPColors.primary500,
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                    ),
                  ),
                ],
              ),
            ),

            // Square map: width x width
            LayoutBuilder(
              builder: (context, constraints) {
                final double size = constraints.maxWidth;
                return SizedBox(
                  width: size,
                  height: size,
                  child: Stack(
                    children: [
                      // map with rounded corners and subtle shadow
                      Container(
                        decoration: BoxDecoration(
                          borderRadius: BorderRadius.circular(12),
                          boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.06), blurRadius: 10, offset: const Offset(0, 6))],
                        ),
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(12),
                          child: GoogleMap(
                            initialCameraPosition: _initialCameraPosition,
                            onMapCreated: (controller) => _mapController = controller,
                            markers: _markers,
                            myLocationEnabled: true,
                            myLocationButtonEnabled: true,
                            zoomControlsEnabled: false,
                          ),
                        ),
                      ),

                      // Camera button (通報災情) inside the map, top-right (white circle with primary icon)
                      Positioned(
                        top: 14,
                        right: 14,
                        child: GestureDetector(
                          onTap: () => Get.to(() => const UploadEventView()),
                          child: Container(
                            width: 52,
                            height: 52,
                            decoration: BoxDecoration(
                              color: Colors.white,
                              shape: BoxShape.circle,
                              boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.12), blurRadius: 8, offset: const Offset(0, 4))],
                            ),
                            child: Center(
                              child: Icon(Icons.camera_alt, color: TPColors.primary500, size: 24),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),

            const SizedBox(height: 16),

            // Two side-by-side buttons below the map (primary + outline)
            Row(
              children: [
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: () => Get.to(() => const EventListView()),
                    icon: const Icon(Icons.event_available),
                    label: const TPText('災害事件列表', style: TPTextStyles.bodySemiBold, color: TPColors.white),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: TPColors.primary500,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: OutlinedButton.icon(
                    onPressed: () => Get.to(() => const ShelterListView()),
                    icon: const Icon(Icons.home_outlined, color: TPColors.primary500),
                    label: const TPText('避難收容處所', style: TPTextStyles.bodySemiBold, color: TPColors.primary500),
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      side: const BorderSide(color: TPColors.primary500),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
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