import 'package:flutter/material.dart';
import 'package:get/get.dart';
import 'package:town_pass/page/disaster_shelter/model/disaster_event.dart';
import 'package:town_pass/page/disaster_shelter/widget/event_card/event_item_widget.dart';
import 'package:town_pass/page/disaster_shelter/widget/event_detail_sheet.dart';
import 'package:town_pass/util/tp_app_bar.dart';
import 'package:town_pass/util/tp_colors.dart';
import 'package:town_pass/util/tp_text.dart';

class EventListView extends StatefulWidget {
  const EventListView({super.key});

  @override
  State<EventListView> createState() => _EventListViewState();
}

class _EventListViewState extends State<EventListView> {
  List<DisasterEvent> allEvents = [];
  List<DisasterEvent> filteredEvents = [];
  bool isLoading = true;
  
  // 查詢控制器
  final TextEditingController searchController = TextEditingController();
  
  // 所有可用的標籤
  final List<String> allTags = [
    '道路阻塞',
    '淹水',
    '停電',
    '火災',
    '建物損壞',
    '人員受困',
    '人員受傷',
    '人員傷亡',
  ];
  
  // 選中的標籤
  Set<String> selectedTags = {};

  @override
  void initState() {
    super.initState();
    _loadEvents();
    searchController.addListener(_filterEvents);
  }

  @override
  void dispose() {
    searchController.dispose();
    super.dispose();
  }

  Future<void> _loadEvents() async {
    setState(() {
      isLoading = true;
    });

    // 模擬 API 呼叫延遲
    await Future.delayed(const Duration(seconds: 1));

    // 載入假資料
    setState(() {
      allEvents = DisasterEvent.getMockData();
      filteredEvents = allEvents;
      isLoading = false;
    });
  }

  void _filterEvents() {
    setState(() {
      filteredEvents = allEvents.where((event) {
        // 搜尋過濾
        final searchText = searchController.text.toLowerCase();
        final matchesSearch = searchText.isEmpty ||
            event.title.toLowerCase().contains(searchText) ||
            event.description.toLowerCase().contains(searchText);

        // 標籤過濾
        final matchesTags = selectedTags.isEmpty ||
            event.tags.any((tag) => selectedTags.contains(tag));

        return matchesSearch && matchesTags;
      }).toList();
    });
  }

  void _toggleTag(String tag) {
    setState(() {
      if (selectedTags.contains(tag)) {
        selectedTags.remove(tag);
      } else {
        selectedTags.add(tag);
      }
      _filterEvents();
    });
  }

  void _clearFilters() {
    setState(() {
      selectedTags.clear();
      searchController.clear();
      _filterEvents();
    });
  }

  void _showEventDetail(DisasterEvent event) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (context) => EventDetailSheet(event: event),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: TPColors.white,
      appBar: const TPAppBar(
        title: '災害事件列表',
      ),
      body: Column(
        children: [
          // 搜尋欄
          Padding(
            padding: const EdgeInsets.all(16),
            child: TextField(
              controller: searchController,
              decoration: InputDecoration(
                hintText: '搜尋事件標題或描述',
                prefixIcon: const Icon(Icons.search, color: TPColors.grayscale500),
                suffixIcon: searchController.text.isNotEmpty
                    ? IconButton(
                        icon: const Icon(Icons.clear, color: TPColors.grayscale500),
                        onPressed: () {
                          searchController.clear();
                          _filterEvents();
                        },
                      )
                    : null,
                filled: true,
                fillColor: TPColors.grayscale50,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                  borderSide: BorderSide.none,
                ),
                contentPadding: const EdgeInsets.symmetric(
                  horizontal: 16,
                  vertical: 12,
                ),
              ),
            ),
          ),

          // 標籤篩選區
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const TPText(
                      '篩選標籤',
                      style: TPTextStyles.h3SemiBold,
                      color: TPColors.grayscale900,
                    ),
                    const Spacer(),
                    if (selectedTags.isNotEmpty)
                      TextButton(
                        onPressed: _clearFilters,
                        child: const TPText(
                          '清除篩選',
                          style: TPTextStyles.bodyRegular,
                          color: TPColors.primary500,
                        ),
                      ),
                  ],
                ),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: allTags.map((tag) {
                    final isSelected = selectedTags.contains(tag);
                    return FilterChip(
                      label: TPText(
                        tag,
                        style: TPTextStyles.bodyRegular,
                        color: isSelected ? TPColors.white : TPColors.grayscale700,
                      ),
                      selected: isSelected,
                      onSelected: (_) => _toggleTag(tag),
                      backgroundColor: TPColors.grayscale100,
                      selectedColor: _getTagColor(tag),
                      checkmarkColor: TPColors.white,
                      side: BorderSide(
                        color: isSelected
                            ? _getTagColor(tag)
                            : TPColors.grayscale300,
                      ),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    );
                  }).toList(),
                ),
                const SizedBox(height: 8),
              ],
            ),
          ),

          // 結果數量顯示
          if (selectedTags.isNotEmpty || searchController.text.isNotEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: Row(
                children: [
                  TPText(
                    '找到 ${filteredEvents.length} 筆結果',
                    style: TPTextStyles.bodyRegular,
                    color: TPColors.grayscale600,
                  ),
                ],
              ),
            ),

          const Divider(height: 1),

          // 事件列表
          Expanded(
            child: isLoading
                ? const Center(
                    child: CircularProgressIndicator(),
                  )
                : filteredEvents.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            const Icon(
                              Icons.search_off_rounded,
                              size: 64,
                              color: TPColors.grayscale400,
                            ),
                            const SizedBox(height: 16),
                            TPText(
                              selectedTags.isNotEmpty || searchController.text.isNotEmpty
                                  ? '沒有符合條件的事件'
                                  : '目前沒有災害事件',
                              style: TPTextStyles.h3Regular,
                              color: TPColors.grayscale500,
                            ),
                            if (selectedTags.isNotEmpty || searchController.text.isNotEmpty)
                              Padding(
                                padding: const EdgeInsets.only(top: 8),
                                child: TextButton(
                                  onPressed: _clearFilters,
                                  child: const TPText(
                                    '清除所有篩選',
                                    style: TPTextStyles.bodyRegular,
                                    color: TPColors.primary500,
                                  ),
                                ),
                              ),
                          ],
                        ),
                      )
                    : RefreshIndicator(
                        onRefresh: _loadEvents,
                        child: ListView.builder(
                          physics: const BouncingScrollPhysics(),
                          itemCount: filteredEvents.length,
                          itemBuilder: (context, index) {
                            final event = filteredEvents[index];
                            return EventItemWidget(
                              event: event,
                              onTap: () => _showEventDetail(event),
                            );
                          },
                        ),
                      ),
          ),
        ],
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