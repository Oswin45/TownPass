import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;
import 'package:google_fonts/google_fonts.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_app_bar.dart';
import 'package:town_pass/util/tp_text.dart';
import 'package:geolocator/geolocator.dart';

class Shelter {
  final String name;
  final String address;
  final int capacity;
  final int currentPeople;
  final String type;
  final int disasterTypes;
  final bool accessible;
  final String phone;
  final double area;
  final double latitude;
  final double longitude;
  double? distance;

  Shelter({
    required this.name,
    required this.address,
    required this.capacity,
    required this.currentPeople,
    required this.type,
    required this.disasterTypes,
    required this.accessible,
    required this.phone,
    required this.area,
    required this.latitude,
    required this.longitude,
    this.distance,
  });

  factory Shelter.fromJson(Map<String, dynamic> json) {
    final supportedDisasters =
        (json['supportedDisasters'] ?? json['disasterTypes'] ?? 0);
    final accessibleRaw = (json['accesibility'] ??
        json['accessibility'] ??
        json['accessible'] ??
        false);
    final phoneRaw = (json['telephone'] ?? json['phone'] ?? '').toString();
    final areaRaw = (json['sizeInSquareMeters'] ?? json['area'] ?? 0);

    return Shelter(
      name: (json['name'] ?? '未命名避難所').toString(),
      address: (json['address'] ?? '無地址資訊').toString(),
      capacity: _asInt(json['capacity']),
      currentPeople: _asInt(
        json['currentPeople'] ?? json['currentOccupancy'],
        defaultValue: 0,
      ),
      type: (json['type'] ?? '未知類型').toString(),
      disasterTypes: _asInt(supportedDisasters),
      accessible: _asBool(accessibleRaw),
      phone: phoneRaw,
      latitude: _asDouble(json['latitude']),
      longitude: _asDouble(json['longitude']),
      area: _asDouble(areaRaw),
    );
  }

  double get percentage =>
      capacity == 0 ? 0 : (currentPeople / capacity).clamp(0, 1);

  static int _asInt(dynamic v, {int defaultValue = 0}) {
    if (v == null) return defaultValue;
    if (v is int) return v;
    if (v is double) return v.toInt();
    final s = v.toString();
    return int.tryParse(s) ?? defaultValue;
  }

  static double _asDouble(dynamic v, {double defaultValue = 0}) {
    if (v == null) return defaultValue;
    if (v is double) return v;
    if (v is int) return v.toDouble();
    final s = v.toString();
    return double.tryParse(s) ?? defaultValue;
  }

  static bool _asBool(dynamic v) {
    if (v is bool) return v;
    final s = v.toString().toLowerCase();
    return s == 'true' || s == '1' || s == 'yes';
  }
}

class ShelterListView extends StatefulWidget {
  const ShelterListView({super.key});

  @override
  State<ShelterListView> createState() => _ShelterListViewState();
}

class _ShelterListViewState extends State<ShelterListView> {
  List<Shelter> shelters = [];
  List<Shelter> filteredShelters = [];
  bool isLoading = true;

  String sortMode = '距離由近到遠';
  final List<String> sortOptions = ['距離由近到遠', '未滿優先'];
  final TextEditingController searchController = TextEditingController();

  final List<Map<String, dynamic>> disasterFilters = [
    {'label': '淹水', 'bit': 1, 'color': TPColors.primary500},
    {'label': '地震', 'bit': 2, 'color': TPColors.orange500},
    {'label': '土石流', 'bit': 4, 'color': TPColors.secondary800},
    {'label': '海嘯', 'bit': 8, 'color': TPColors.primary700},
    {'label': '防空', 'bit': 16, 'color': TPColors.primary900},
  ];
  Set<int> selectedDisasters = {};

  Position? _currentPosition;

  @override
  void initState() {
    super.initState();
    _loadShelters();
    _getUserLocation();
  }

  Future<void> _getUserLocation() async {
    try {
      LocationPermission permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.deniedForever) return;
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) return;
      }
      _currentPosition = await Geolocator.getCurrentPosition(
          desiredAccuracy: LocationAccuracy.high);
      debugPrint('📍使用者位置：${_currentPosition!.latitude}, ${_currentPosition!.longitude}');
    } catch (e) {
      debugPrint('❌ 無法取得位置: $e');
    }
  }

  Future<void> _loadShelters() async {
    setState(() => isLoading = true);
    try {
      final jsonStr =
          await rootBundle.loadString('assets/mock_data/shelter.json');
      final dynamic data = jsonDecode(jsonStr);
      final list = _extractFirstList(data);
      final parsed = list
          .whereType<Map<String, dynamic>>()
          .map((e) => Shelter.fromJson(e))
          .toList();

      setState(() {
        shelters = parsed;
        filteredShelters = shelters;
        isLoading = false;
      });
      debugPrint('✅ 成功載入 ${shelters.length} 筆避難所資料');
    } catch (e, st) {
      debugPrint('❌ shelter.json 載入/解析失敗: $e');
      debugPrint(st.toString());
      setState(() {
        isLoading = false;
        shelters = [];
        filteredShelters = [];
      });
    }
  }

  List<dynamic> _extractFirstList(dynamic data) {
    if (data is List) return data;
    if (data is Map) {
      const likelyKeys = ['shelters', 'Shelters', 'data', 'items', 'result'];
      for (final k in likelyKeys) {
        final v = data[k];
        if (v is List) return v;
      }
      for (final v in data.values) {
        if (v is List) return v;
      }
    }
    return const [];
  }

  void _filterShelters(String query) {
    final q = query.trim();
    setState(() {
      filteredShelters = shelters.where((shelter) {
        final matchesQuery = q.isEmpty ||
            shelter.name.contains(q) ||
            shelter.address.contains(q);
        final matchesDisaster = selectedDisasters.isEmpty ||
            selectedDisasters.any((bit) => (shelter.disasterTypes & bit) != 0);
        return matchesQuery && matchesDisaster;
      }).toList();
      _sortShelters();
    });
  }

  void _toggleDisasterFilter(int bit) {
    setState(() {
      if (selectedDisasters.contains(bit)) {
        selectedDisasters.remove(bit);
      } else {
        selectedDisasters.add(bit);
      }
      _filterShelters(searchController.text);
    });
  }

  void _clearAllFilters() {
    setState(() {
      selectedDisasters.clear();
      searchController.clear();
      _filterShelters('');
    });
  }

  void _sortShelters() async {
    if (sortMode == '距離由近到遠') {
      // 取得使用者位置
      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
      );
      final userLat = position.latitude;
      final userLng = position.longitude;

      // 計算每個 shelter 的距離
      for (final s in filteredShelters) {
        s.distance = Geolocator.distanceBetween(
          userLat,
          userLng,
          s.latitude,
          s.longitude,
        );
      }

      // 排序距離
      filteredShelters.sort((a, b) {
        final d1 = a.distance ?? double.infinity;
        final d2 = b.distance ?? double.infinity;
        return d1.compareTo(d2);
      });

      setState(() {}); // 刷新畫面
    }
    else if (sortMode == '未滿優先') {
      // 顏色 + 距離排序（保留原有邏輯）
      filteredShelters.sort((a, b) {
        int getPriority(double p) {
          if (p < 0.6) return 0; // 綠
          if (p < 0.9) return 1; // 黃
          return 2; // 紅
        }

        final pA = getPriority(a.percentage);
        final pB = getPriority(b.percentage);
        if (pA != pB) return pA.compareTo(pB);
        return (a.distance ?? double.infinity)
            .compareTo(b.distance ?? double.infinity);
      });
    }
  }

  Color _getStatusColor(double percentage) {
    if (percentage < 0.6) return TPColors.primary500;
    if (percentage < 0.9) return TPColors.orange500;
    return TPColors.red500;
  }

  String _disasterTypesToString(int flag) {
    final types = <String>[];
    if (flag & 1 != 0) types.add('淹水');
    if (flag & 2 != 0) types.add('地震');
    if (flag & 4 != 0) types.add('土石流');
    if (flag & 8 != 0) types.add('海嘯');
    if (flag & 16 != 0) types.add('防空');
    return types.isEmpty ? '無' : types.join('、');
  }

  void _showShelterDetail(Shelter shelter) {
    showModalBottomSheet(
      context: context,
      backgroundColor: Colors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      isScrollControlled: true,
      builder: (context) => Padding(
        padding: const EdgeInsets.only(top: 20, left: 20, right: 20, bottom: 40),
        child: SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 40,
                  height: 4,
                  margin: const EdgeInsets.only(bottom: 20),
                  decoration: BoxDecoration(
                    color: TPColors.grayscale300,
                    borderRadius: BorderRadius.circular(2),
                  ),
                ),
              ),
              Text(
                shelter.name,
                style: GoogleFonts.notoSans(
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                  color: TPColors.grayscale900,
                ),
              ),
              const SizedBox(height: 8),
              // ✅ 地址 + 距離並排
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Text(
                      shelter.address,
                      style: const TextStyle(color: TPColors.grayscale600),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  if (shelter.distance != null)
                    Text(
                      shelter.distance! < 1000
                          ? '距離：${shelter.distance!.toStringAsFixed(0)} 公尺'
                          : '距離：${(shelter.distance! / 1000).toStringAsFixed(2)} 公里',
                      style: const TextStyle(
                          color: TPColors.grayscale600, fontSize: 13),
                    ),
                ],
              ),
              const SizedBox(height: 6),
              // ✅ 目前人數 / 總人數
              Text(
                '目前人數：${shelter.currentPeople}/${shelter.capacity}',
                style: const TextStyle(
                  color: TPColors.grayscale700,
                  fontSize: 14,
                  fontWeight: FontWeight.w500,
                ),
              ),
              const SizedBox(height: 20),
              _buildDetailRow('避難收容處所類型', shelter.type),
              _buildDetailRow(
                  '支援災害類型', _disasterTypesToString(shelter.disasterTypes)),
              _buildDetailRow('是否有無障礙設施', shelter.accessible ? '有' : '無'),
              _buildDetailRow(
                  '聯絡電話', shelter.phone.isEmpty ? '—' : shelter.phone),
              _buildDetailRow(
                  '面積', shelter.area > 0 ? '${shelter.area} 平方公尺' : '—'),
              const SizedBox(height: 28),
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: () {},
                      icon: const Icon(Icons.map_outlined, color: Colors.white),
                      label: const Text('查看地圖'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: TPColors.primary500,
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () {},
                      icon: const Icon(Icons.navigation_rounded),
                      label: const Text('前往導航'),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12)),
                        side: const BorderSide(color: TPColors.primary500),
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildDetailRow(String title, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Expanded(
            flex: 3,
            child: Text(title,
                style: GoogleFonts.notoSans(
                    color: TPColors.grayscale700, fontWeight: FontWeight.w600)),
          ),
          Expanded(
            flex: 5,
            child: Text(value, style: const TextStyle(color: TPColors.grayscale900)),
          ),
        ],
      ),
    );
  }

  @override
Widget build(BuildContext context) {
  return Scaffold(
    backgroundColor: TPColors.white,
    appBar: const TPAppBar(
      title: '避難收容處所列表',
    ),
      body: isLoading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                _buildSearchBar(),
                _buildSortDropdown(),
                _buildFilterChips(),
                _buildShelterList(),
              ],
            ),
    );
  }

  Widget _buildSearchBar() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: TextField(
        controller: searchController,
        onChanged: _filterShelters,
        decoration: InputDecoration(
          hintText: '搜尋避難收容處所名稱或地址',
          prefixIcon: const Icon(Icons.search),
          filled: true,
          fillColor: TPColors.grayscale50,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: BorderSide.none,
          ),
        ),
      ),
    );
  }

  Widget _buildSortDropdown() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: DropdownButtonFormField<String>(
        value: sortMode,
        onChanged: (v) {
          if (v != null) {
            setState(() => sortMode = v);
            _sortShelters();
          }
        },
        items: sortOptions
            .map((e) => DropdownMenuItem(value: e, child: Text(e)))
            .toList(),
        decoration: InputDecoration(
          labelText: '排序方式',
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
          contentPadding:
              const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
        ),
      ),
    );
  }

Widget _buildFilterChips() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Text(
                '支援災害類型',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  color: TPColors.grayscale900,
                ),
              ),
              const SizedBox(width: 8),
              Text(
                '(${filteredShelters.length}筆)',
                style: const TextStyle(
                  fontSize: 14,
                  color: TPColors.grayscale500,
                ),
              ),
              const Spacer(),
              if (selectedDisasters.isNotEmpty)
                TextButton(
                  onPressed: _clearAllFilters,
                  child: const Text(
                    '清除篩選',
                    style: TextStyle(
                      fontSize: 14,
                      color: TPColors.primary500,
                    ),
                  ),
                ),
            ],
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 16,
            children: disasterFilters.map((filter) {
              final isSelected = selectedDisasters.contains(filter['bit']);
              final Color color = filter['color'] as Color;
              return Container(
                height: 32,
                child: FilterChip(
                  materialTapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  labelPadding: const EdgeInsets.symmetric(horizontal: 8),
                  label: Text(
                    filter['label'] as String,
                    style: TextStyle(
                      color: isSelected ? Colors.white : TPColors.grayscale700,
                      fontSize: 14,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                  selected: isSelected,
                  onSelected: (_) => _toggleDisasterFilter(filter['bit'] as int),
                  backgroundColor: TPColors.grayscale100,
                  selectedColor: color,
                  checkmarkColor: Colors.transparent,
                  showCheckmark: false,
                  visualDensity: VisualDensity.compact,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                    side: BorderSide(
                      color: isSelected ? color : TPColors.grayscale200,
                      width: 1,
                    ),
                  ),
                ),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }

  Widget _buildShelterList() {
    return Expanded(
      child: ListView.builder(
        itemCount: filteredShelters.length,
        itemBuilder: (context, index) {
          final s = filteredShelters[index];
          final color = _getStatusColor(s.percentage);

          return Card(
            margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            elevation: 3,
            child: ListTile(
              onTap: () => _showShelterDetail(s),
              leading: Icon(Icons.circle, color: color, size: 14),
              title: Text(
                s.name,
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  color: TPColors.grayscale900,
                ),
              ),
              // ✅ 地址 + 距離並排
              subtitle: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Text(
                      s.address,
                      style: const TextStyle(color: TPColors.grayscale700),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  if (s.distance != null)
                    Text(
                      s.distance! < 1000
                          ? '距離：${s.distance!.toStringAsFixed(0)} 公尺'
                          : '距離：${(s.distance! / 1000).toStringAsFixed(2)} 公里',
                      style: const TextStyle(
                          color: TPColors.grayscale600, fontSize: 13),
                    ),
                ],
              ),
              trailing: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    '${s.currentPeople}/${s.capacity}',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      color: TPColors.grayscale900,
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildStatusDot(double percentage) {
    final color = _getStatusColor(percentage);
    return Container(
      width: 16,
      height: 16,
      decoration: BoxDecoration(
        color: color,
        shape: BoxShape.circle,
        boxShadow: [
          BoxShadow(
            color: color.withOpacity(0.3),
            blurRadius: 6,
            offset: const Offset(0, 2),
          ),
        ],
      ),
    );
  }
}
