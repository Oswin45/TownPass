import 'package:flutter/material.dart';
import 'package:town_pass/util/tp_app_bar.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_text.dart';

class ShelterListView extends StatelessWidget {
  const ShelterListView({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: TPColors.white,
      appBar: const TPAppBar(
        title: '避難收容處所',
      ),
      body: const Center(
        child: Text('這裡顯示避難收容處所列表（範例頁面）'),
      ),
    );
  }
}
