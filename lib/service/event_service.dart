import 'dart:convert';
import 'dart:io';
import 'package:flutter/services.dart';
import 'package:path_provider/path_provider.dart';
import 'package:town_pass/page/disaster_shelter/model/disaster_event.dart';

class EventService {
  static const String _assetPath = 'assets/mock_data/events.json';
  static const String _fileName = 'events.json';

  /// 獲取本地存儲路徑
  Future<String> get _localPath async {
    final directory = await getApplicationDocumentsDirectory();
    return directory.path;
  }

  /// 獲取本地文件
  Future<File> get _localFile async {
    final path = await _localPath;
    return File('$path/$_fileName');
  }

  /// 初始化:從 assets 複製到本地(首次使用)
  Future<void> initialize() async {
    try {
      final file = await _localFile;
      if (!await file.exists()) {
        final data = await rootBundle.loadString(_assetPath);
        await file.writeAsString(data);
      }
    } catch (e) {
      print('初始化事件資料失敗: $e');
    }
  }

  /// 讀取所有事件
  Future<List<DisasterEvent>> loadEvents() async {
    try {
      final file = await _localFile;
      
      if (!await file.exists()) {
        await initialize();
      }

      final contents = await file.readAsString();
      final List<dynamic> jsonData = json.decode(contents);
      
      return jsonData
          .map((json) => DisasterEvent.fromJson(json as Map<String, dynamic>))
          .toList();
    } catch (e) {
      print('讀取事件資料失敗: $e');
      return [];
    }
  }

  /// 儲存所有事件
  Future<bool> saveEvents(List<DisasterEvent> events) async {
    try {
      final file = await _localFile;
      final jsonData = events.map((e) => e.toJson()).toList();
      final jsonString = json.encode(jsonData);
      await file.writeAsString(jsonString);
      return true;
    } catch (e) {
      print('儲存事件資料失敗: $e');
      return false;
    }
  }

  /// 新增事件
  Future<bool> addEvent(DisasterEvent event) async {
    try {
      final events = await loadEvents();
      events.add(event);
      return await saveEvents(events);
    } catch (e) {
      print('新增事件失敗: $e');
      return false;
    }
  }

  /// 更新事件
  Future<bool> updateEvent(DisasterEvent event) async {
    try {
      final events = await loadEvents();
      final index = events.indexWhere((e) => e.id == event.id);
      
      if (index != -1) {
        events[index] = event;
        return await saveEvents(events);
      }
      return false;
    } catch (e) {
      print('更新事件失敗: $e');
      return false;
    }
  }

  /// 刪除事件
  Future<bool> deleteEvent(String id) async {
    try {
      final events = await loadEvents();
      events.removeWhere((e) => e.id == id);
      return await saveEvents(events);
    } catch (e) {
      print('刪除事件失敗: $e');
      return false;
    }
  }
}