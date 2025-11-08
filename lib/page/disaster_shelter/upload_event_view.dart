import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:geolocator/geolocator.dart';
import 'package:dio/dio.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../../util/tp_colors.dart';

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
    bool isValid = true;
    setState(() {
      _showTitleError = _titleController.text.isEmpty;
      _showDescError = _descController.text.isEmpty;
      _showImageError = _image == null;
      isValid = !_showTitleError && !_showDescError && !_showImageError;
    });
    if (!isValid) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('請填寫所有必要資訊'),
          backgroundColor: TPColors.red500,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
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
      // TODO: 實際上傳邏輯
      await Future.delayed(const Duration(seconds: 2));

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('上傳成功'),
          backgroundColor: TPColors.grayscale900,
          behavior: SnackBarBehavior.floating,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          margin: const EdgeInsets.all(10),
        ),
      );
      _deleteData();
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: const Text('上傳失敗，請重試'),
          backgroundColor: TPColors.red500,
        ),
      );
    } finally {
      setState(() => _isUploading = false);
    }
  }

  Future<void> _confirmDelete() async {
    final bool? confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('確認清除'),
        content: const Text('是否要清除所有已輸入的資料？'),
        actions: [
          TextButton(
            child: const Text('取消'),
            onPressed: () => Navigator.of(context).pop(false),
          ),
          TextButton(
            child: const Text('確認'),
            onPressed: () => Navigator.of(context).pop(true),
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
    final imageHeight = (screenWidth - 32) * 3 / 4;

    return Scaffold(
      appBar: AppBar(
        title: Text(
          "上傳災情",
          style: GoogleFonts.notoSans(
            fontSize: 20,
            fontWeight: FontWeight.w500,
            color: TPColors.grayscale900, // 深灰色
          ),
        ),
        backgroundColor: TPColors.white,
        elevation: 0,
        leading: IconButton(
          icon: Icon(
            Icons.arrow_back_ios,
            color: TPColors.grayscale900, // 深灰色
            size: 20,
          ),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      backgroundColor: TPColors.white, // 背景白色
      body: SingleChildScrollView(
        padding: const EdgeInsets.only(left: 30, right: 30, top: 18, bottom: 14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: GestureDetector(
                onTap: () async {
                  showModalBottomSheet(
                    context: context,
                    backgroundColor: TPColors.white,
                    shape: const RoundedRectangleBorder(
                      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
                    ),
                    builder: (context) => SafeArea(
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          ListTile(
                            leading: Icon(Icons.camera_alt,
                                color: TPColors.grayscale900),
                            title: Text('拍照',
                                style: TextStyle(color: TPColors.grayscale900)),
                            onTap: () {
                              Navigator.pop(context);
                              _pickImage(ImageSource.camera);
                            },
                          ),
                          ListTile(
                            leading: Icon(Icons.photo_library,
                                color: TPColors.grayscale900),
                            title: Text('從圖庫選擇',
                                style: TextStyle(color: TPColors.grayscale900)),
                            onTap: () {
                              Navigator.pop(context);
                              _pickImage(ImageSource.gallery);
                            },
                          ),
                        ],
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
                          : TPColors.grayscale900,
                      width: 2,
                    ),
                    borderRadius: BorderRadius.circular(12),
                    color: TPColors.white,
                    boxShadow: [
                      BoxShadow(
                        color: TPColors.black.withOpacity(0.1),
                        blurRadius: 4,
                        offset: const Offset(0, 2),
                      ),
                    ],
                  ),
                  child: _image == null
                      ? Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.add_a_photo,
                                size: 48,
                                color: _showImageError
                                    ? TPColors.red500
                                    : TPColors.grayscale900),
                            const SizedBox(height: 12),
                            Text(
                              "點擊以拍照或選取圖片",
                              style: TextStyle(
                                color: _showImageError
                                    ? TPColors.red500
                                    : TPColors.grayscale900,
                                fontSize: 16,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ],
                        )
                      : ClipRRect(
                          borderRadius: BorderRadius.circular(12),
                          child: Image.file(_image!, fit: BoxFit.cover),
                        ),
                ),
              ),
            ),
            const SizedBox(height: 20),

            _buildInputField(
              controller: _titleController,
              label: "請輸入標題",
              showError: _showTitleError,
            ),
            const SizedBox(height: 16),
            _buildInputField(
              controller: _descController,
              label: "請輸入描述",
              showError: _showDescError,
              maxLines: 3,
            ),
            const SizedBox(height: 20),

            Text(
              "選擇標籤（最多3個）：",
              style: GoogleFonts.notoSans(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: TPColors.grayscale900,
              ),
            ),
            const SizedBox(height: 12),

            Container(
              width: double.infinity,
              decoration: BoxDecoration(
                color: TPColors.white,
                borderRadius: BorderRadius.circular(12),
                boxShadow: [
                  BoxShadow(
                    color: TPColors.black.withOpacity(0.1),
                    blurRadius: 4,
                    offset: const Offset(0, 2),
                  ),
                ],
              ),
              child: GridView.builder(
                padding: const EdgeInsets.all(12),
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                  crossAxisCount: 4,
                  childAspectRatio: 2.2,
                  crossAxisSpacing: 8,
                  mainAxisSpacing: 8,
                ),
                itemCount: tagOptions.length,
                itemBuilder: (context, index) {
                  final tag = tagOptions[index];
                  final isSelected = selectedTags.contains(tag);
                  return Material(
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
                              ),
                            );
                          }
                        });
                      },
                      borderRadius: BorderRadius.circular(20),
                      child: Container(
                        decoration: BoxDecoration(
                          color: isSelected
                              ? TPColors.grayscale900
                              : TPColors.white,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(
                            color: isSelected
                                ? TPColors.grayscale900
                                : TPColors.grayscale200,
                          ),
                        ),
                        alignment: Alignment.center,
                        child: Text(
                          tag,
                          style: TextStyle(
                            color: isSelected
                                ? TPColors.white
                                : TPColors.grayscale900,
                            fontSize: 12,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                    ),
                  );
                },
              ),
            ),
            const SizedBox(height: 32),

            if (_isUploading)
              const Center(
                child: CircularProgressIndicator(
                  valueColor: AlwaysStoppedAnimation<Color>(
                      TPColors.grayscale900),
                ),
              )
            else
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                children: [
                  ElevatedButton.icon(
                    onPressed: _sendData,
                    icon: const Icon(Icons.send, color: Colors.white, size: 20),
                    label: const Text("送出",
                        style: TextStyle(
                            color: Colors.white,
                            fontSize: 16,
                            fontWeight: FontWeight.w500)),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: TPColors.grayscale900,
                      padding: const EdgeInsets.symmetric(
                          horizontal: 40, vertical: 12),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(25)),
                      elevation: 2,
                    ),
                  ),
                  ElevatedButton.icon(
                    onPressed: _confirmDelete,
                    icon: Icon(Icons.delete_outline,
                        color: TPColors.grayscale900, size: 20),
                    label: Text("清除",
                        style: TextStyle(
                            color: TPColors.grayscale900,
                            fontSize: 16,
                            fontWeight: FontWeight.w500)),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: TPColors.white,
                      padding: const EdgeInsets.symmetric(
                          horizontal: 40, vertical: 12),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(25),
                        side: BorderSide(color: TPColors.grayscale900),
                      ),
                      elevation: 0,
                    ),
                  ),
                ],
              ),
            const SizedBox(height: 20),
          ],
        ),
      ),
    );
  }

  Widget _buildInputField({
    required TextEditingController controller,
    required String label,
    required bool showError,
    int maxLines = 1,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: TPColors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: TPColors.black.withOpacity(0.1),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: TextField(
        controller: controller,
        maxLines: maxLines,
        decoration: InputDecoration(
          labelText: label,
          labelStyle: TextStyle(
            color: showError ? TPColors.red500 : TPColors.grayscale900,
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: BorderSide(
              color: showError ? TPColors.red500 : TPColors.grayscale900,
            ),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: BorderSide(
              color: showError ? TPColors.red500 : TPColors.grayscale900,
              width: 2,
            ),
          ),
        ),
      ),
    );
  }
}