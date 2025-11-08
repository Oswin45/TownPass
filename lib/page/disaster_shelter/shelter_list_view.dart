import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_app_bar.dart';

// ✅ 模擬資料結構
class Shelter {
  final String name;
  final double distance; // 公里
  final int currentPeople;
  final int capacity;
  final String type;
  final List<String> disasterSupport;
  final bool accessible;
  final String phone;
  final String area; // m²

  Shelter({
    required this.name,
    required this.distance,
    required this.currentPeople,
    required this.capacity,
    required this.type,
    required this.disasterSupport,
    required this.accessible,
    required this.phone,
    required this.area,
  });

  double get percentage => currentPeople / capacity;
}

// ✅ 主頁面：避難所列表
class ShelterListView extends StatefulWidget {
  const ShelterListView({super.key});

  @override
  State<ShelterListView> createState() => _ShelterListViewState();
}

class _ShelterListViewState extends State<ShelterListView> {
  String _sortOption = '由近到遠';

  final List<Shelter> shelters = [
    Shelter(
      name: '中正國小體育館',
      distance: 0.8,
      currentPeople: 120,
      capacity: 400,
      type: '學校避難所',
      disasterSupport: ['地震', '颱風'],
      accessible: true,
      phone: '02-2345-6789',
      area: '800',
    ),
    Shelter(
      name: '大安森林公園管理中心',
      distance: 1.2,
      currentPeople: 450,
      capacity: 500,
      type: '公園避難所',
      disasterSupport: ['火災', '地震'],
      accessible: true,
      phone: '02-2721-3306',
      area: '1200',
    ),
    Shelter(
      name: '松山社區活動中心',
      distance: 2.5,
      currentPeople: 180,
      capacity: 200,
      type: '社區避難所',
      disasterSupport: ['地震', '豪雨'],
      accessible: false,
      phone: '02-2255-7890',
      area: '600',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    List<Shelter> sortedShelters = List.from(shelters);
    if (_sortOption == '由近到遠') {
      sortedShelters.sort((a, b) => a.distance.compareTo(b.distance));
    } else if (_sortOption == '未滿優先') {
      sortedShelters.sort((a, b) => a.percentage.compareTo(b.percentage));
    }

    return Scaffold(
      backgroundColor: TPColors.white,
      appBar: AppBar(
        backgroundColor: TPColors.white,
        elevation: 0,
        title: Text(
          '避難所列表',
          style: GoogleFonts.notoSans(
            fontSize: 18,
            fontWeight: FontWeight.w600,
            color: TPColors.grayscale900,
          ),
        ),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_ios, size: 18, color: TPColors.grayscale700),
          onPressed: () => Navigator.pop(context),
        ),
        actions: [
          Padding(
            padding: const EdgeInsets.only(right: 12),
            child: DropdownButtonHideUnderline(
              child: DropdownButton<String>(
                value: _sortOption,
                icon: const Icon(Icons.arrow_drop_down, color: TPColors.grayscale700),
                items: const [
                  DropdownMenuItem(value: '由近到遠', child: Text('由近到遠')),
                  DropdownMenuItem(value: '未滿優先', child: Text('未滿優先')),
                ],
                onChanged: (value) {
                  setState(() => _sortOption = value!);
                },
              ),
            ),
          ),
        ],
      ),
      body: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: sortedShelters.length,
        itemBuilder: (context, index) {
          final shelter = sortedShelters[index];
          final color = _getStatusColor(shelter.percentage);

          return GestureDetector(
            onTap: () => _showShelterDetail(context, shelter),
            child: Card(
              elevation: 2,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              margin: const EdgeInsets.only(bottom: 16),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Row(
                  children: [
                    // 🟢🟡🔴 狀態燈
                    Container(
                      width: 18,
                      height: 18,
                      decoration: BoxDecoration(
                        color: color,
                        shape: BoxShape.circle,
                        boxShadow: [
                          BoxShadow(
                            color: color.withOpacity(0.4),
                            blurRadius: 6,
                            spreadRadius: 1,
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            shelter.name,
                            style: GoogleFonts.notoSans(
                              fontSize: 16,
                              fontWeight: FontWeight.w600,
                              color: TPColors.grayscale900,
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(
                            '距離 ${shelter.distance.toStringAsFixed(1)} 公里',
                            style: GoogleFonts.notoSans(
                              fontSize: 14,
                              color: TPColors.grayscale600,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const Icon(Icons.chevron_right, color: TPColors.grayscale400),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    );
  }

  Color _getStatusColor(double percentage) {
    if (percentage < 0.6) return TPColors.primary500;
    if (percentage < 0.9) return TPColors.orange500;
    return TPColors.red500;
  }

  void _showShelterDetail(BuildContext context, Shelter shelter) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: TPColors.white,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) {
        return Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
          child: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // 標題
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
                    fontWeight: FontWeight.w600,
                    color: TPColors.grayscale900,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  '距離 ${shelter.distance.toStringAsFixed(1)} 公里',
                  style: GoogleFonts.notoSans(
                    fontSize: 14,
                    color: TPColors.grayscale600,
                  ),
                ),
                const SizedBox(height: 20),

                _buildInfoRow('避難所類型', shelter.type),
                _buildInfoRow('支援災害類型', shelter.disasterSupport.join('、')),
                _buildInfoRow('是否有無障礙設施', shelter.accessible ? '有' : '無'),
                _buildInfoRow('聯絡電話', shelter.phone),
                _buildInfoRow('面積（平方公尺）', shelter.area),
                const SizedBox(height: 32),

                // 按鈕區
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: () {},
                        icon: const Icon(Icons.map_outlined, color: TPColors.primary500),
                        label: const Text(
                          '在地圖上顯示',
                          style: TextStyle(color: TPColors.primary500),
                        ),
                        style: OutlinedButton.styleFrom(
                          side: const BorderSide(color: TPColors.primary500, width: 1.5),
                          padding: const EdgeInsets.symmetric(vertical: 12),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                      ),
                    ),
                    const SizedBox(width: 16),
                    Expanded(
                      child: ElevatedButton.icon(
                        onPressed: () {},
                        icon: const Icon(Icons.directions, color: Colors.white),
                        label: const Text(
                          '前往導航',
                          style: TextStyle(color: Colors.white),
                        ),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: TPColors.primary500,
                          padding: const EdgeInsets.symmetric(vertical: 12),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildInfoRow(String title, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 120,
            child: Text(
              title,
              style: GoogleFonts.notoSans(
                fontSize: 15,
                fontWeight: FontWeight.w500,
                color: TPColors.grayscale700,
              ),
            ),
          ),
          Expanded(
            child: Text(
              value,
              style: GoogleFonts.notoSans(
                fontSize: 15,
                color: TPColors.grayscale900,
              ),
            ),
          ),
        ],
      ),
    );
  }
}