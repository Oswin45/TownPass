import 'dart:io';
import 'dart:async';
import 'dart:typed_data';
import 'dart:convert';
import 'package:flutter/material.dart';
import 'dart:ui' as ui;
import 'package:flutter_image_compress/flutter_image_compress.dart';
import 'package:image_picker/image_picker.dart';
import 'package:geolocator/geolocator.dart';
import 'package:dio/dio.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../util/tp_colors.dart';
import 'package:town_pass/util/tp_app_bar.dart';
// (tp_text not used here)

class UploadEventView extends StatefulWidget {
  const UploadEventView({Key? key}) : super(key: key);

  @override
  State<UploadEventView> createState() => _UploadEventViewState();
}

class _UploadEventViewState extends State<UploadEventView> {
  File? _image;
  final TextEditingController _titleController = TextEditingController();
  final TextEditingController _descController = TextEditingController();
  bool _isUploading = false;
  bool _showTitleError = false;
  bool _showDescError = false;
  bool _showImageError = false;
  final Set<String> selectedTags = <String>{};

  final List<String> tagOptions = [
    '道路阻塞',
    '淹水',
    '停電',
    '火災',
    '建物損壞',
    '人員受困',
    '人員受傷',
    '人員傷亡',
  ];

  Future<void> _pickImage(ImageSource source) async {
    try {
      final ImagePicker picker = ImagePicker();
      final XFile? pickedFile =
          await picker.pickImage(source: source, imageQuality: 85);
      if (pickedFile != null) {
        setState(() {
          _image = File(pickedFile.path);
          _showImageError = false;
        });
      }
    } catch (e) {
      debugPrint('Error picking image: $e');
    }
  }

  bool _validateForm() {
    setState(() {
      _showTitleError = _titleController.text.isEmpty;
      _showDescError = _descController.text.isEmpty;
      _showImageError = _image == null;
    });
    final isValid = !_showTitleError && !_showDescError && !_showImageError;
    if (!isValid) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('請填寫所有必要資訊'),
          backgroundColor: TPColors.red500,
          behavior: SnackBarBehavior.floating,
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          margin: const EdgeInsets.all(10),
        ),
      );
    }
    return isValid;
  }

  Future<void> _sendData() async {
    if (!_validateForm()) return;
    setState(() => _isUploading = true);

    try {
      // 1) 檢查定位服務與權限
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text('請先開啟定位服務以取得座標'),
            backgroundColor: TPColors.red500,
            behavior: SnackBarBehavior.floating,
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            margin: const EdgeInsets.all(10),
          ),
        );
        setState(() => _isUploading = false);
        return;
      }

      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text('需允許定位權限以送出災情'),
            backgroundColor: TPColors.red500,
            behavior: SnackBarBehavior.floating,
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            margin: const EdgeInsets.all(10),
          ),
        );
        setState(() => _isUploading = false);
        return;
      }

      // 2) 取得目前位置
      final Position position = await Geolocator.getCurrentPosition(
          desiredAccuracy: LocationAccuracy.best);

      // 3) 壓縮（目標高度 720p，保持長寬比）並轉換圖片為 base64
      if (_image == null) {
        // 這裡理論上不會發生，因為 _validateForm 已檢查
        throw Exception('No image selected');
      }
      final originalBytes = await _image!.readAsBytes();

      // helper: decode image to get original dimensions
      Future<ui.Image> _decodeImage(Uint8List data) {
        final completer = Completer<ui.Image>();
        ui.decodeImageFromList(data, (ui.Image img) {
          completer.complete(img);
        });
        return completer.future;
      }

      final ui.Image originalImage = await _decodeImage(originalBytes);
      final int origW = originalImage.width;
      final int origH = originalImage.height;

      // 目標高度（pix）
      const int targetH = 720;
      // 計算對應寬度以維持長寬比
      int targetW = ((origW / origH) * targetH).round();
      // 若影像本身小於等於目標高度，不放大
      if (origH <= targetH) {
        targetW = origW;
      }

      // 嘗試壓縮檔案（若壓縮失敗，退回使用原始 bytes）
      Uint8List? compressedBytes;
      try {
        compressedBytes = await FlutterImageCompress.compressWithFile(
          _image!.path,
          minWidth: targetW,
          minHeight: targetH <= origH ? targetH : origH,
          quality: 85,
          keepExif: true,
        );
      } catch (_) {
        compressedBytes = null;
      }

      final bytesToUse = compressedBytes ?? originalBytes;
      final base64Image = base64Encode(bytesToUse);

      // 4) 組 payload
      final payload = {
        'img': base64Image,
        'tags': selectedTags.toList(),
        'description': _descController.text,
        'title': _titleController.text,
        'lnt': position.longitude,
        'lat': position.latitude,
      };

      // 5) 發送到後端 API
      // Debug: 在送出前把要送出的 body 印到 terminal（只顯示 img preview 與長度以免過大）
      final int previewLen = base64Image.length > 200 ? 200 : base64Image.length;
      final String imgPreview = base64Image.substring(0, previewLen) +
          (base64Image.length > 200 ? '...' : '');
      final debugPayload = {
        ...payload,
        'img': 'BASE64(len=${base64Image.length}, preview=${imgPreview})'
      };
      debugPrint('Uploading payload: ${jsonEncode(debugPayload)}');

      final dioClient = Dio();
      final response = await dioClient.post(
        'https://shelter.sausagee.party/api/DisasterEvent',
        data: payload,
        options: Options(
          headers: {'Content-Type': 'application/json'},
          responseType: ResponseType.json,
        ),
      );

      // Debug: 印出回應 status 與 body
      debugPrint('DisasterEvent response status: ${response.statusCode}');
      debugPrint('DisasterEvent response data: ${response.data}');

      // 6) 處理回應
      final respData = response.data;
      final success = respData != null && (respData['success'] == true || respData['success'] == 'true');
      if (success) {
        debugPrint('DisasterEvent: success = true, data=${respData}');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: const Text('上傳成功'),
            backgroundColor: TPColors.primary500,
            behavior: SnackBarBehavior.floating,
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            margin: const EdgeInsets.all(10),
          ),
        );
        _deleteData();
      } else {
        final message = (respData != null && respData['message'] != null)
            ? respData['message'].toString()
            : '上傳失敗，請重試';
        // Debug: 印出 server failure 訊息
        debugPrint('DisasterEvent: success = false, status=${response.statusCode}, message=$message, data=${respData}');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(message),
            backgroundColor: TPColors.red500,
            behavior: SnackBarBehavior.floating,
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            margin: const EdgeInsets.all(10),
          ),
        );
      }
    } on DioError catch (dioErr) {
      // Debug: 印出 Dio 錯誤詳細
      debugPrint('DioError: ${dioErr.toString()}');
      debugPrint('DioError response: status=${dioErr.response?.statusCode}, data=${dioErr.response?.data}');
      String msg = '上傳失敗，請重試';
      if (dioErr.response != null && dioErr.response?.data != null) {
        final data = dioErr.response?.data;
        if (data is Map && data['message'] != null) msg = data['message'].toString();
      } else if (dioErr.message != null && dioErr.message!.isNotEmpty) {
        msg = dioErr.message!;
      }
      debugPrint('DioError user message: $msg');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(msg),
          backgroundColor: TPColors.red500,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          margin: const EdgeInsets.all(10),
        ),
      );
    } catch (e) {
      // Debug: 印出非 Dio 的例外
      debugPrint('Upload exception: ${e.toString()}');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('上傳失敗：${e.toString()}'),
          backgroundColor: TPColors.red500,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          margin: const EdgeInsets.all(10),
        ),
      );
    } finally {
      setState(() => _isUploading = false);
    }
  }

  Future<void> _confirmDelete() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Text(
          '確認清除',
          style: GoogleFonts.notoSans(
            fontSize: 18,
            fontWeight: FontWeight.w600,
            color: TPColors.grayscale900,
          ),
        ),
        content: Text(
          '是否要清除所有已輸入的資料？',
          style: GoogleFonts.notoSans(
            fontSize: 15,
            color: TPColors.grayscale700,
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(
              '取消',
              style: TextStyle(
                color: TPColors.grayscale600,
                fontSize: 15,
              ),
            ),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(
              '確認',
              style: TextStyle(
                color: TPColors.red500,
                fontSize: 15,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
    if (confirm == true) _deleteData();
  }

  void _deleteData() {
    setState(() {
      _image = null;
      _titleController.clear();
      _descController.clear();
      selectedTags.clear();
      _showTitleError = false;
      _showDescError = false;
      _showImageError = false;
    });
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;
    final imageHeight = (screenWidth - 64) * 3 / 4;

    return Scaffold(
      appBar: const TPAppBar(
        title: "上傳災情",
      ),
      backgroundColor: TPColors.grayscale50,
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // 📸 上傳圖片 - 優化設計
            Center(
              child: GestureDetector(
                onTap: () {
                  showModalBottomSheet(
                    context: context,
                    backgroundColor: TPColors.white,
                    shape: const RoundedRectangleBorder(
                      borderRadius:
                          BorderRadius.vertical(top: Radius.circular(20)),
                    ),
                    builder: (context) => SafeArea(
                      child: Padding(
                        padding: const EdgeInsets.symmetric(vertical: 12),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Container(
                              width: 40,
                              height: 4,
                              margin: const EdgeInsets.only(bottom: 20),
                              decoration: BoxDecoration(
                                color: TPColors.grayscale300,
                                borderRadius: BorderRadius.circular(2),
                              ),
                            ),
                            ListTile(
                              leading: Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: TPColors.primary500.withOpacity(0.1),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Icon(Icons.camera_alt,
                                    color: TPColors.primary500, size: 24),
                              ),
                              title: Text('拍照',
                                  style: GoogleFonts.notoSans(
                                      color: TPColors.grayscale900,
                                      fontSize: 16,
                                      fontWeight: FontWeight.w500)),
                              onTap: () {
                                Navigator.pop(context);
                                _pickImage(ImageSource.camera);
                              },
                            ),
                            const SizedBox(height: 4),
                            ListTile(
                              leading: Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: TPColors.secondary500.withOpacity(0.1),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Icon(Icons.photo_library,
                                    color: TPColors.secondary500, size: 24),
                              ),
                              title: Text('從圖庫選擇',
                                  style: GoogleFonts.notoSans(
                                      color: TPColors.grayscale900,
                                      fontSize: 16,
                                      fontWeight: FontWeight.w500)),
                              onTap: () {
                                Navigator.pop(context);
                                _pickImage(ImageSource.gallery);
                              },
                            ),
                            const SizedBox(height: 8),
                          ],
                        ),
                      ),
                    ),
                  );
                },
                child: Container(
                  width: double.infinity,
                  height: imageHeight,
                  decoration: BoxDecoration(
                    border: Border.all(
                      color: _showImageError
                          ? TPColors.red500
                          : TPColors.grayscale200,
                      width: 2,
                    ),
                    borderRadius: BorderRadius.circular(16),
                    color: TPColors.white,
                    boxShadow: [
                      BoxShadow(
                        color: _showImageError
                            ? TPColors.red500.withOpacity(0.1)
                            : TPColors.grayscale900.withOpacity(0.05),
                        blurRadius: 12,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  child: _image == null
                      ? Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Container(
                              padding: const EdgeInsets.all(20),
                              decoration: BoxDecoration(
                                color: _showImageError
                                    ? TPColors.red500.withOpacity(0.1)
                                    : TPColors.primary500.withOpacity(0.1),
                                shape: BoxShape.circle,
                              ),
                              child: Icon(Icons.add_a_photo,
                                  size: 40,
                                  color: _showImageError
                                      ? TPColors.red500
                                      : TPColors.primary500),
                            ),
                            const SizedBox(height: 16),
                            Text(
                              "點擊以拍照或選取圖片",
                              style: GoogleFonts.notoSans(
                                  color: _showImageError
                                      ? TPColors.red500
                                      : TPColors.grayscale600,
                                  fontSize: 15,
                                  fontWeight: FontWeight.w500),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              "支援 JPEG、HEIC 格式",
                              style: GoogleFonts.notoSans(
                                  color: TPColors.grayscale400, fontSize: 13),
                            ),
                          ],
                        )
                      : Stack(
                          children: [
                            ClipRRect(
                              borderRadius: BorderRadius.circular(14),
                              child: Image.file(_image!, fit: BoxFit.cover),
                            ),
                            Positioned(
                              top: 12,
                              right: 12,
                              child: Container(
                                padding: const EdgeInsets.all(8),
                                decoration: BoxDecoration(
                                  color: TPColors.grayscale900.withOpacity(0.7),
                                  shape: BoxShape.circle,
                                ),
                                child: Icon(Icons.edit,
                                    color: TPColors.white, size: 20),
                              ),
                            ),
                          ],
                        ),
                ),
              ),
            ),
            const SizedBox(height: 28),

            // ✏️ 標題輸入
            _buildInputField(
              controller: _titleController,
              label: "災情標題",
              hint: "請簡短描述災情狀況",
              showError: _showTitleError,
              icon: Icons.title,
              maxLines: 1,
            ),
            const SizedBox(height: 20),

            // 🗒️ 描述輸入
            _buildInputField(
              controller: _descController,
              label: "詳細描述",
              hint: "請詳細描述災情狀況",
              showError: _showDescError,
              icon: Icons.description,
              maxLines: 4,
            ),
            const SizedBox(height: 28),

            // 🏷️ 標籤 GridView
            Text(
              "災情標籤",
              style: GoogleFonts.notoSans(
                fontSize: 17,
                fontWeight: FontWeight.w600,
                color: TPColors.grayscale900,
              ),
            ),
            const SizedBox(height: 6),
            Text(
              "選擇最多 3 個相關標籤",
              style: GoogleFonts.notoSans(
                fontSize: 14,
                color: TPColors.grayscale500,
              ),
            ),
            const SizedBox(height: 16),

            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: TPColors.white,
                borderRadius: BorderRadius.circular(16),
                boxShadow: [
                  BoxShadow(
                    color: TPColors.grayscale900.withOpacity(0.04),
                    blurRadius: 10,
                    offset: const Offset(0, 2),
                  ),
                ],
              ),
              child: GridView.builder(
                padding: EdgeInsets.zero,
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 4,
                  childAspectRatio: 2.0,
                  crossAxisSpacing: 10,
                  mainAxisSpacing: 10,
                ),
                itemCount: tagOptions.length,
                itemBuilder: (context, index) {
                  final tag = tagOptions[index];
                  final isSelected = selectedTags.contains(tag);
                  return Container(
                    decoration: BoxDecoration(
                      color:
                          isSelected ? _getTagColor(tag) : TPColors.grayscale50,
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: isSelected
                            ? _getTagColor(tag)
                            : TPColors.grayscale200,
                        width: 1.5,
                      ),
                      boxShadow: isSelected
                          ? [
                              BoxShadow(
                                color: _getTagColor(tag).withOpacity(0.3),
                                blurRadius: 6,
                                offset: const Offset(0, 2),
                              ),
                            ]
                          : [],
                    ),
                    child: Material(
                      color: Colors.transparent,
                      child: InkWell(
                        onTap: () {
                          setState(() {
                            if (isSelected) {
                              selectedTags.remove(tag);
                            } else if (selectedTags.length < 3) {
                              selectedTags.add(tag);
                            } else {
                              ScaffoldMessenger.of(context).showSnackBar(
                                SnackBar(
                                  content: const Text('最多只能選三個標籤'),
                                  backgroundColor: TPColors.red500,
                                  behavior: SnackBarBehavior.floating,
                                  shape: RoundedRectangleBorder(
                                      borderRadius: BorderRadius.circular(10)),
                                  margin: const EdgeInsets.all(10),
                                ),
                              );
                            }
                          });
                        },
                        borderRadius: BorderRadius.circular(10),
                        child: Center(
                          child: Text(
                            tag,
                            style: GoogleFonts.notoSans(
                              color: isSelected
                                  ? TPColors.white
                                  : TPColors.grayscale700,
                              fontSize: 13,
                              fontWeight: isSelected
                                  ? FontWeight.w600
                                  : FontWeight.w500,
                            ),
                            textAlign: TextAlign.center,
                          ),
                        ),
                      ),
                    ),
                  );
                },
              ),
            ),
            const SizedBox(height: 36),

            // 🔘 按鈕區
            if (_isUploading)
              Center(
                child: Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: TPColors.white,
                    shape: BoxShape.circle,
                    boxShadow: [
                      BoxShadow(
                        color: TPColors.primary500.withOpacity(0.2),
                        blurRadius: 12,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  child: const CircularProgressIndicator(
                    valueColor:
                        AlwaysStoppedAnimation<Color>(TPColors.primary500),
                    strokeWidth: 3,
                  ),
                ),
              )
            else
              Row(
                children: [
                  Expanded(
                    flex: 3,
                    child: ElevatedButton(
                      onPressed: _sendData,
                      style: ElevatedButton.styleFrom(
                        backgroundColor: TPColors.primary500,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                        ),
                        elevation: 0,
                        shadowColor: TPColors.primary500.withOpacity(0.4),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          const Icon(Icons.send_rounded,
                              color: Colors.white, size: 20),
                          const SizedBox(width: 8),
                          Text(
                            "送出",
                            style: GoogleFonts.notoSans(
                              color: Colors.white,
                              fontSize: 16,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    flex: 2,
                    child: OutlinedButton(
                      onPressed: _confirmDelete,
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(14),
                        ),
                        side: BorderSide(
                            color: TPColors.grayscale300, width: 1.5),
                        backgroundColor: TPColors.white,
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.refresh_rounded,
                              color: TPColors.grayscale700, size: 20),
                          const SizedBox(width: 6),
                          Text(
                            "清除",
                            style: GoogleFonts.notoSans(
                              color: TPColors.grayscale700,
                              fontSize: 15,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }

  Widget _buildInputField({
    required TextEditingController controller,
    required String label,
    required String hint,
    required bool showError,
    required IconData icon,
    int maxLines = 1,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: TPColors.white,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [
          BoxShadow(
            color: showError
                ? TPColors.red500.withOpacity(0.1)
                : TPColors.grayscale900.withOpacity(0.04),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: TextField(
        controller: controller,
        maxLines: maxLines,
        style: GoogleFonts.notoSans(
          fontSize: 15,
          color: TPColors.grayscale900,
        ),
        decoration: InputDecoration(
          labelText: label,
          hintText: hint,
          labelStyle: GoogleFonts.notoSans(
            color: showError ? TPColors.red500 : TPColors.grayscale500,
            fontSize: 14,
            fontWeight: FontWeight.w500,
          ),
          hintStyle: GoogleFonts.notoSans(
            color: TPColors.grayscale400,
            fontSize: 14,
          ),
          prefixIcon: Icon(
            icon,
            color: showError ? TPColors.red500 : TPColors.grayscale400,
            size: 22,
          ),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: BorderSide(
              color: showError ? TPColors.red500 : TPColors.grayscale200,
              width: 1.5,
            ),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: BorderSide(
              color: showError ? TPColors.red500 : TPColors.grayscale200,
              width: 1.5,
            ),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: BorderSide(
              color: showError ? TPColors.red500 : TPColors.primary500,
              width: 2,
            ),
          ),
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 16,
            vertical: 16,
          ),
          filled: true,
          fillColor: TPColors.white,
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
