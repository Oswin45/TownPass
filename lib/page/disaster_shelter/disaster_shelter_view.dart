import 'package:flutter/material.dart';
import 'package:get/get.dart';
import 'package:town_pass/page/disaster_shelter/event_list_view.dart';
import 'package:town_pass/page/disaster_shelter/upload_event_view.dart';
import 'package:town_pass/util/tp_app_bar.dart';
import 'package:town_pass/util/tp_button.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_text.dart';

class DisasterShelterView extends StatefulWidget {
  const DisasterShelterView({super.key});

  @override
  State<DisasterShelterView> createState() => _DisasterShelterViewState();
}

class _DisasterShelterViewState extends State<DisasterShelterView> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: TPColors.white,
      appBar: const TPAppBar(
        title: '避難導航',
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            _buildFeatureCard(
              title: '災害事件列表',
              description: '查看目前發生的災害事件',
              icon: Icons.warning_amber_rounded,
              color: TPColors.red500,
              onTap: () => Get.to(() => const EventListView()),
            ),
            const SizedBox(height: 16),
            _buildFeatureCard(
              title: '避難收容處所',
              description: '尋找附近的避難收容處所',
              icon: Icons.home_rounded,
              color: TPColors.primary500,
              onTap: () {
                // TODO: 導航到避難所列表
              },
            ),
            const SizedBox(height: 16),
            _buildFeatureCard(
              title: '通報災情',
              description: '上傳災害現場資訊',
              icon: Icons.upload_rounded,
              color: TPColors.orange500,
              onTap: () {
                Get.to(() => const UploadEventView());
              },
            ),
    
          ],
        ),
      ),
    );
  }

  Widget _buildFeatureCard({
    required String title,
    required String description,
    required IconData icon,
    required Color color,
    required VoidCallback onTap,
  }) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              Container(
                width: 56,
                height: 56,
                decoration: BoxDecoration(
                  color: color.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(icon, size: 32, color: color),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    TPText(
                      title,
                      style: TPTextStyles.h3SemiBold,
                      color: TPColors.grayscale900,
                    ),
                    const SizedBox(height: 4),
                    TPText(
                      description,
                      style: TPTextStyles.bodyRegular,
                      color: TPColors.grayscale600,
                    ),
                  ],
                ),
              ),
              Icon(Icons.chevron_right, color: TPColors.grayscale400),
            ],
          ),
        ),
      ),
    );
  }
}
