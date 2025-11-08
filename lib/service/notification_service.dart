import 'dart:async';
import 'dart:io';

import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:get/get.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_route.dart';

class NotificationService extends GetxService {
  static int _id = 0;
  static final FlutterLocalNotificationsPlugin _notificationInstance =
      FlutterLocalNotificationsPlugin();

  final RxBool isDisasterMode = false.obs;
  final RxString currentDisasterType = ''.obs;

  Future<NotificationService> init() async {
    await _notificationInstance.getNotificationAppLaunchDetails();

    final InitializationSettings initializationSettings =
        InitializationSettings(
      android: const AndroidInitializationSettings('@mipmap/ic_launcher'),
      iOS: DarwinInitializationSettings(
        requestAlertPermission: false,
        requestBadgePermission: false,
        requestSoundPermission: false,
        onDidReceiveLocalNotification:
            (int id, String? title, String? body, String? payload) async {
          // add action when notification clicked
        },
      ),
    );

    await _notificationInstance.initialize(initializationSettings,
        onDidReceiveNotificationResponse: _onNotificationTapped);

    return this;
  }

  static Future<void> requestPermission() async {
    if (Platform.isAndroid) {
      await _notificationInstance
          .resolvePlatformSpecificImplementation<
              AndroidFlutterLocalNotificationsPlugin>()
          ?.requestNotificationsPermission();
    } else if (Platform.isIOS) {
      await _notificationInstance
          .resolvePlatformSpecificImplementation<
              IOSFlutterLocalNotificationsPlugin>()
          ?.requestPermissions(
            alert: true,
            badge: true,
            sound: true,
          );
    }
  }

  static Future<void> showNotification({String? title, String? content}) async {
    await _notificationInstance.show(
      _id++,
      title,
      content,
      const NotificationDetails(
        android: AndroidNotificationDetails(
          'TownPass android notification id',
          'TownPass android notification channel name',
          importance: Importance.max,
          priority: Priority.max,
        ),
      ),
    );
  }

  void _onNotificationTapped(NotificationResponse response) {
    final payload = response.payload;
    if (payload != null) {
      final parts = payload.split('|');
      if (parts.length >= 2) {
        final type = parts[0]; // disaster
        final disasterType = parts[1]; // 火災、淹水等

        if (type == 'disaster') {
          isDisasterMode.value = true;
          currentDisasterType.value = disasterType;

          // Get.toNamed(TPRoute.disasterShelter);
          Get.back();
        }
      }
    }
  }

  Future<void> sendDisasterNotification({
    required String title,
    required String body,
    required String disasterType, // 火災、淹水、地震等
  }) async {
    const AndroidNotificationDetails androidDetails =
        AndroidNotificationDetails(
      'disaster_channel',
      '災害通知',
      channelDescription: '緊急災害警報通知',
      importance: Importance.max,
      priority: Priority.high,
      playSound: true,
      enableVibration: true,
      enableLights: true,
      color: TPColors.white, // 紅色
      icon: '@mipmap/ic_launcher',
    );

    const DarwinNotificationDetails iosDetails = DarwinNotificationDetails(
      presentAlert: true,
      presentBadge: true,
      presentSound: true,
      sound: 'default',
    );

    const NotificationDetails notificationDetails = NotificationDetails(
      android: androidDetails,
      iOS: iosDetails,
    );

    await _notificationInstance.show(
      DateTime.now().millisecondsSinceEpoch ~/ 1000,
      title,
      body,
      notificationDetails,
      payload: 'disaster|$disasterType',
    );
  }

  Future<void> sendTestDisasterNotification(String disasterType) async {
    final Map<String, Map<String, String>> disasterMessages = {
      '淹水': {
        'title': '🌊 淹水警報',
        'body': '您所在區域有淹水危險，請立即前往地勢較高的避難所！水位持續上升中。',
      },
      '地震': {
        'title': '🌍 地震速報',
        'body': '偵測到地震活動，震度可能達5級以上！請立即掩護並前往安全地點。',
      },
      '土石流': {
        'title': '⛰️ 土石流警戒',
        'body': '您附近山區有土石流危險，請儘速撤離至安全避難所！',
      },
      '海嘯': {
        'title': '🌊 海嘯警報',
        'body': '海嘯警報發布！沿海地區居民請立即往高處疏散，遠離海岸線！',
      },
      '防空': {
        'title': '🚨 防空警報',
        'body': '防空警報！請立即前往最近的防空避難所，保持冷靜並遵循指示。',
      },
    };

    final message = disasterMessages[disasterType] ??
        {
          'title': '⚠️ 災害警報',
          'body': '偵測到災害事件，請注意安全！',
        };

    await sendDisasterNotification(
      title: message['title']!,
      body: message['body']!,
      disasterType: disasterType,
    );
  }

  // 清除災害模式
  void clearDisasterMode() {
    isDisasterMode.value = false;
    currentDisasterType.value = '';
  }
  Future<void> cancelAllNotifications() async {
    await _notificationInstance.cancelAll();
  }
}
