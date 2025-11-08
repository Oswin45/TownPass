import 'package:flutter/material.dart';
import 'package:town_pass/page/disaster_shelter/model/disaster_event.dart';
import 'package:town_pass/util/tp_cached_network_image.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_text.dart';

class EventItemWidget extends StatelessWidget {
  final DisasterEvent event;
  final VoidCallback onTap;

  const EventItemWidget({
    super.key,
    required this.event,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // 圖片
            ClipRRect(
              borderRadius: const BorderRadius.vertical(
                top: Radius.circular(12),
              ),
              child: AspectRatio(
                aspectRatio: 16 / 9,
                child: TPCachedNetworkImage(
                  imageUrl: event.img,
                  fit: BoxFit.cover,
                ),
              ),
            ),
            
            Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // 標籤
                  if (event.tags.isNotEmpty)
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: event.tags.take(3).map((tag) {
                        return Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 8,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: _getTagColor(tag),
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: TPText(
                            tag,
                            style: TPTextStyles.bodyRegular,
                            color: TPColors.white,
                          ),
                        );
                      }).toList(),
                    ),
                  
                  const SizedBox(height: 12),
                  
                  // 標題
                  Row(
                    children: [
                      Expanded(
                        child: TPText(
                          event.title,
                          style: TPTextStyles.h3SemiBold,
                          color: TPColors.grayscale900,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      const SizedBox(width: 8),
                      const Icon(
                        Icons.chevron_right,
                        color: TPColors.grayscale400,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
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