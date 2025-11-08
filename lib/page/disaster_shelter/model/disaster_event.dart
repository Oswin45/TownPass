class DisasterEvent {
  final String id;
  final String img;
  final List<String> tags;
  final String title;
  final String description;
  final String lat;
  final String lng;

  DisasterEvent({
    required this.id,
    required this.img,
    required this.tags,
    required this.title,
    required this.description,
    required this.lat,
    required this.lng,
  });

  // JSON 序列化
  factory DisasterEvent.fromJson(Map<String, dynamic> json) {
    return DisasterEvent(
      id: json['id'] as String,
      img: json['img'] as String,
      tags: List<String>.from(json['tags'] as List),
      title: json['title'] as String,
      description: json['description'] as String,
      lat: json['lat'] as String,
      lng: json['lng'] as String,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'img': img,
      'tags': tags,
      'title': title,
      'description': description,
      'lat': lat,
      'lng': lng,
    };
  }

  // 用於更新事件
  DisasterEvent copyWith({
    String? id,
    String? img,
    List<String>? tags,
    String? title,
    String? description,
    String? lat,
    String? lng,
  }) {
    return DisasterEvent(
      id: id ?? this.id,
      img: img ?? this.img,
      tags: tags ?? this.tags,
      title: title ?? this.title,
      description: description ?? this.description,
      lat: lat ?? this.lat,
      lng: lng ?? this.lng,
    );
  }
}
