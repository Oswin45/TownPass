import 'package:flutter/material.dart';
import 'package:town_pass/page/disaster_shelter/model/disaster_event.dart';
import 'package:town_pass/util/tp_cached_network_image.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_text.dart';

class EventDetailSheet extends StatelessWidget {
  final DisasterEvent event;

  const EventDetailSheet({
    super.key,
    required this.event,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(top: 200),
      decoration: const BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.vertical(
          top: Radius.circular(20),
        ),
      ),
      child: DraggableScrollableSheet(
        initialChildSize: 1.0,
        minChildSize: 1.0,
        maxChildSize: 1.0,
        builder: (context, scrollController) {
          return Column(
            children: [
              // 拖曳指示器
              Container(
                margin: const EdgeInsets.symmetric(vertical: 12),
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: TPColors.grayscale300,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              
              Expanded(
                child: ListView(
                  controller: scrollController,
                  padding: const EdgeInsets.all(16),
                  children: [
                    // 圖片
                    ClipRRect(
                      borderRadius: BorderRadius.circular(12),
                      child: AspectRatio(
                        aspectRatio: 16 / 9,
                        child: TPCachedNetworkImage(
                          imageUrl: event.img,
                          fit: BoxFit.cover,
                        ),
                      ),
                    ),
                    
                    const SizedBox(height: 16),
                    
                    // 標籤
                    if (event.tags.isNotEmpty)
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: event.tags.map((tag) {
                          return Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 12,
                              vertical: 6,
                            ),
                            decoration: BoxDecoration(
                              color: _getTagColor(tag),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: TPText(
                              tag,
                              style: TPTextStyles.bodyRegular,
                              color: TPColors.white,
                            ),
                          );
                        }).toList(),
                      ),
                    
                    const SizedBox(height: 16),
                    
                    // 標題
                    TPText(
                      event.title,
                      style: TPTextStyles.h2SemiBold,
                      color: TPColors.grayscale900,
                    ),
                    
                    const SizedBox(height: 12),
                    
                    // 描述
                    TPText(
                      event.description,
                      style: TPTextStyles.bodyRegular,
                      color: TPColors.grayscale700,
                    ),
                    
                    const SizedBox(height: 24),
                    
                    // 位置資訊卡片
                    Container(
                      padding: const EdgeInsets.all(16),
                      decoration: BoxDecoration(
                        color: TPColors.grayscale50,
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(
                          color: TPColors.grayscale200,
                          width: 1,
                        ),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const Row(
                            children: [
                              Icon(
                                Icons.location_on,
                                color: TPColors.primary500,
                                size: 24,
                              ),
                              SizedBox(width: 8),
                              TPText(
                                '位置資訊',
                                style: TPTextStyles.h3SemiBold,
                                color: TPColors.grayscale900,
                              ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          _InfoRow(
                            label: '緯度',
                            value: event.lat,
                          ),
                          const SizedBox(height: 8),
                          _InfoRow(
                            label: '經度',
                            value: event.lng,
                          ),
                        ],
                      ),
                    ),
                    
                    const SizedBox(height: 16),
                    
                    // 操作按鈕
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton.icon(
                            onPressed: () {
                              // TODO: 在地圖上顯示
                            },
                            style: OutlinedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(vertical: 12),
                              side: const BorderSide(color: TPColors.primary500),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(8),
                              ),
                            ),
                            icon: const Icon(
                              Icons.map_outlined,
                              color: TPColors.primary500,
                            ),
                            label: const TPText(
                              '在地圖上顯示',
                              style: TPTextStyles.bodyRegular,
                              color: TPColors.primary500,
                            ),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: ElevatedButton.icon(
                            onPressed: () {
                              // TODO: 導航到此位置
                            },
                            style: ElevatedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(vertical: 12),
                              backgroundColor: TPColors.primary500,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(8),
                              ),
                            ),
                            icon: const Icon(
                              Icons.directions,
                              color: Colors.white,
                            ),
                            label: const TPText(
                              '前往導航',
                              style: TPTextStyles.bodyRegular,
                              color: Colors.white,
                            ),
                          ),
                        ),
                      ],
                    ),
                    
                    const SizedBox(height: 16),
                  ],
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Color _getTagColor(String tag) {
    switch (tag) {
      case '道路阻塞':
        return TPColors.secondary500;
      case '淹水':
        return TPColors.primary500;
      case '停電':
        return TPColors.orange500;
      case '火災':
        return TPColors.red500;
      case '建物損壞':
        return TPColors.secondary800;
      case '人員受困':
        return TPColors.orange700;
      case '人員受傷':
        return TPColors.red700;
      case '人員傷亡':
        return TPColors.red900;
      default:
        return TPColors.grayscale500;
    }
  }
}

class _InfoRow extends StatelessWidget {
  final String label;
  final String value;

  const _InfoRow({
    required this.label,
    required this.value,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        TPText(
          '$label: ',
          style: TPTextStyles.bodyRegular,
          color: TPColors.grayscale600,
        ),
        Expanded(
          child: TPText(
            value,
            style: TPTextStyles.bodySemiBold,
            color: TPColors.grayscale900,
          ),
        ),
      ],
    );
  }
}