import 'package:flutter/material.dart';
import 'package:get/get.dart';
import 'package:town_pass/service/notification_service.dart';
import 'package:town_pass/util/tp_app_bar.dart';
import 'package:town_pass/util/tp_button.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_text.dart';

class TestNotificationView extends StatelessWidget {
  const TestNotificationView({super.key});

  @override
  Widget build(BuildContext context) {
    final NotificationService notificationService =
        Get.find<NotificationService>();

    final List<Map<String, dynamic>> disasterTypes = [
      {'type': '淹水', 'icon': Icons.water_drop, 'color': TPColors.primary500},
      {'type': '地震', 'icon': Icons.vibration, 'color': TPColors.orange500},
      {'type': '土石流', 'icon': Icons.landslide, 'color': TPColors.red700},
      {'type': '海嘯', 'icon': Icons.tsunami, 'color': TPColors.primary700},
      {'type': '防空', 'icon': Icons.shield_outlined, 'color': TPColors.red900},
    ];

    return Scaffold(
      backgroundColor: TPColors.white,
      appBar: const TPAppBar(title: '測試災害通知'),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // 說明卡片
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: TPColors.primary50,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: TPColors.primary200),
              ),
              child: const Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(Icons.info_outline, color: TPColors.primary500),
                      SizedBox(width: 8),
                      TPText(
                        '測試說明',
                        style: TPTextStyles.h3SemiBold,
                        color: TPColors.primary700,
                      ),
                    ],
                  ),
                  SizedBox(height: 8),
                  TPText(
                    '點擊下方按鈕發送測試災害通知。\n收到通知後點擊，系統會：\n1. 進入災害模式\n2. 導航到避難導航頁面\n3. 顯示最近的避難所',
                    style: TPTextStyles.bodyRegular,
                    color: TPColors.primary700,
                  ),
                ],
              ),
            ),

            const SizedBox(height: 24),

            const TPText(
              '選擇災害類型',
              style: TPTextStyles.h3SemiBold,
              color: TPColors.grayscale900,
            ),

            const SizedBox(height: 16),

            // 災害類型按鈕
            Expanded(
              child: GridView.builder(
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 2,
                  crossAxisSpacing: 12,
                  mainAxisSpacing: 12,
                  childAspectRatio: 1.5,
                ),
                itemCount: disasterTypes.length,
                itemBuilder: (context, index) {
                  final disaster = disasterTypes[index];
                  return _DisasterTypeCard(
                    type: disaster['type'],
                    icon: disaster['icon'],
                    color: disaster['color'],
                    onTap: () async {
                      await notificationService.sendTestDisasterNotification(
                        disaster['type'],
                      );
                      Get.snackbar(
                        '通知已發送',
                        '請查看通知欄',
                        snackPosition: SnackPosition.BOTTOM,
                        backgroundColor: TPColors.primary500,
                        colorText: Colors.white,
                        duration: const Duration(seconds: 2),
                      );
                    },
                  );
                },
              ),
            ),

            const SizedBox(height: 16),

            // 清除災害模式按鈕
            Obx(() {
              if (notificationService.isDisasterMode.value) {
                return Column(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: TPColors.red50,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: TPColors.red200),
                      ),
                      child: Row(
                        children: [
                          Icon(Icons.warning_amber_rounded,
                              color: TPColors.red500, size: 20),
                          const SizedBox(width: 8),
                          Expanded(
                            child: TPText(
                              '目前處於災害模式：${notificationService.currentDisasterType.value}',
                              style: TPTextStyles.bodyRegular,
                              color: TPColors.red700,
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 12),
                    TPButton.primary(
                      text: '清除災害模式',
                      onPressed: () {
                        notificationService.clearDisasterMode();
                        Get.snackbar(
                          '已清除',
                          '災害模式已關閉',
                          snackPosition: SnackPosition.BOTTOM,
                          backgroundColor: TPColors.grayscale700,
                          colorText: Colors.white,
                          duration: const Duration(seconds: 2),
                          margin: const EdgeInsets.all(16),
                        );
                      },
                    ),
                  ],
                );
              }
              return const SizedBox.shrink();
            }),
          ],
        ),
      ),
    );
  }
}

class _DisasterTypeCard extends StatelessWidget {
  final String type;
  final IconData icon;
  final Color color;
  final VoidCallback onTap;

  const _DisasterTypeCard({
    required this.type,
    required this.icon,
    required this.color,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Container(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, size: 40, color: color),
              const SizedBox(height: 8),
              TPText(
                type,
                style: TPTextStyles.h3SemiBold,
                color: TPColors.grayscale900,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
