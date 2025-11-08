import 'package:flutter/material.dart';

class DisasterShelterView extends StatefulWidget {
  const DisasterShelterView({super.key});

  @override
  State<DisasterShelterView> createState() => _DisasterShelterViewState();
}

class _DisasterShelterViewState extends State<DisasterShelterView> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('避難導航'),
      ),
      body: const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text('避難收容處所資訊'),
            SizedBox(height: 16),
            Text('災害發生時提供避難路線'),
            // TODO: 在這裡加入您的微服務功能
          ],
        ),
      ),
    );
  }
}