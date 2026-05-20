import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../domain/entities/tour_entity.dart';
import '../providers/tour_providers.dart';
import '../../../../core/utils/image_picker_utils.dart';

class CreateTourScreen extends ConsumerStatefulWidget {
  final String? tourId; // null = create, non-null = edit
  const CreateTourScreen({super.key, this.tourId});

  @override
  ConsumerState<CreateTourScreen> createState() => _CreateTourScreenState();
}

class _CreateTourScreenState extends ConsumerState<CreateTourScreen> {
  final _formKey = GlobalKey<FormState>();
  final _titleCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  final _locationCtrl = TextEditingController();
  final _priceCtrl = TextEditingController();
  final _durationCtrl = TextEditingController();
  final _maxGuestsCtrl = TextEditingController(text: '10');
  bool _loading = false;
  bool _loadingTour = false;
  TourEntity? _currentTour;
  List<String> _selectedImages = []; // Store image URLs/paths

  bool get _isEdit => widget.tourId != null;

  @override
  void initState() {
    super.initState();
    if (_isEdit) {
      _loadTourData();
    }
  }

  Future<void> _loadTourData() async {
    setState(() => _loadingTour = true);
    try {
      final useCase = ref.read(getTourByIdUseCaseProvider);
      final result = await useCase(widget.tourId!);

      result.fold(
        (failure) {
          if (mounted) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(
                  'Không thể tải thông tin tour: ${failure.message}',
                ),
                backgroundColor: Colors.red,
              ),
            );
            Navigator.pop(context);
          }
        },
        (tour) {
          _currentTour = tour;
          _populateForm(tour);
        },
      );
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: $e'), backgroundColor: Colors.red),
        );
        Navigator.pop(context);
      }
    } finally {
      if (mounted) setState(() => _loadingTour = false);
    }
  }

  void _populateForm(TourEntity tour) {
    _titleCtrl.text = tour.title;
    _descCtrl.text = tour.description ?? '';
    _locationCtrl.text = tour.location;
    _priceCtrl.text = tour.price.toInt().toString();
    _durationCtrl.text = tour.durationHours.toString();
    _maxGuestsCtrl.text = tour.maxParticipants.toString();
    // Note: Existing images from server are kept, new images can be added
  }

  Future<void> _pickImages() async {
    try {
      // Placeholder implementation until image_picker is properly installed
      // final List<XFile> images = await _picker.pickMultiImage(
      //   maxWidth: 1920,
      //   maxHeight: 1080,
      //   imageQuality: 85,
      // );

      // For now, show a dialog to simulate image selection
      final result = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Chọn hình ảnh'),
          content: const Text(
            'Tính năng chọn hình ảnh đang được phát triển. '
            'Bạn có muốn thêm hình ảnh mẫu không?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Hủy'),
            ),
            TextButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Thêm mẫu'),
            ),
          ],
        ),
      );

      if (result == true) {
        setState(() {
          _selectedImages.add('sample_image_${_selectedImages.length + 1}.jpg');
          // Limit to 5 images total
          if (_selectedImages.length > 5) {
            _selectedImages = _selectedImages.take(5).toList();
          }
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Lỗi chọn ảnh: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  void _removeImage(int index) {
    setState(() {
      _selectedImages.removeAt(index);
    });
  }

  @override
  void dispose() {
    _titleCtrl.dispose();
    _descCtrl.dispose();
    _locationCtrl.dispose();
    _priceCtrl.dispose();
    _durationCtrl.dispose();
    _maxGuestsCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);

    try {
      final ds = ref.read(tourRemoteDataSourceProvider);

      // Convert selected images to URLs (for now, we'll use placeholder URLs)
      // In production, you would upload images to a storage service first
      List<String> imageUrls = [];

      // Keep existing images if editing
      if (_isEdit && _currentTour != null) {
        imageUrls.addAll(_currentTour!.images);
      }

      // Add placeholder URLs for new images (in production, upload to storage first)
      for (int i = 0; i < _selectedImages.length; i++) {
        imageUrls.add(
          'https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800&h=600&fit=crop&crop=center',
        );
      }

      if (_isEdit) {
        await ds.updateTour(
          tourId: widget.tourId!,
          title: _titleCtrl.text.trim(),
          description: _descCtrl.text.trim().isEmpty
              ? null
              : _descCtrl.text.trim(),
          location: _locationCtrl.text.trim(),
          price: double.parse(_priceCtrl.text),
          durationHours: int.parse(_durationCtrl.text),
          maxParticipants: int.parse(_maxGuestsCtrl.text),
          images: imageUrls.isNotEmpty ? imageUrls : null,
        );
      } else {
        await ds.createTour(
          guideId: '',
          title: _titleCtrl.text.trim(),
          description: _descCtrl.text.trim().isEmpty
              ? null
              : _descCtrl.text.trim(),
          location: _locationCtrl.text.trim(),
          price: double.parse(_priceCtrl.text),
          durationHours: int.parse(_durationCtrl.text),
          maxParticipants: int.parse(_maxGuestsCtrl.text),
          images: imageUrls,
        );
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              _isEdit
                  ? 'Đã cập nhật tour thành công!'
                  : 'Đã tạo tour thành công!',
            ),
            backgroundColor: Colors.green,
          ),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString()), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loadingTour) {
      return Scaffold(
        backgroundColor: Colors.white,
        appBar: AppBar(
          title: const Text('Đang tải...'),
          backgroundColor: Colors.white,
          foregroundColor: Colors.black,
          elevation: 0,
        ),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      backgroundColor: Colors.white,
      appBar: AppBar(
        title: Text(
          _isEdit ? 'Chỉnh sửa tour' : 'Tạo tour mới',
          style: const TextStyle(fontWeight: FontWeight.bold),
        ),
        backgroundColor: Colors.white,
        foregroundColor: Colors.black,
        elevation: 0,
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            if (_isEdit && _currentTour != null) ...[
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.blue.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.blue.shade200),
                ),
                child: Row(
                  children: [
                    Icon(
                      Icons.info_outline,
                      size: 16,
                      color: Colors.blue.shade700,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Đang chỉnh sửa tour: ${_currentTour!.title}',
                        style: TextStyle(
                          fontSize: 12,
                          color: Colors.blue.shade700,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 16),
            ],

            _Field(
              ctrl: _titleCtrl,
              label: 'Tiêu đề tour *',
              hint: 'VD: Khám phá Phố Cổ Hà Nội',
              validator: (v) => v!.isEmpty ? 'Vui lòng nhập tiêu đề' : null,
            ),
            const SizedBox(height: 16),
            _Field(
              ctrl: _locationCtrl,
              label: 'Địa điểm *',
              hint: 'VD: Hà Nội',
              validator: (v) => v!.isEmpty ? 'Vui lòng nhập địa điểm' : null,
            ),
            const SizedBox(height: 16),
            _Field(
              ctrl: _descCtrl,
              label: 'Mô tả',
              hint: 'Mô tả chi tiết về tour...',
              maxLines: 4,
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _Field(
                    ctrl: _priceCtrl,
                    label: 'Giá (₫) *',
                    hint: '350000',
                    keyboardType: TextInputType.number,
                    validator: (v) {
                      if (v!.isEmpty) return 'Nhập giá';
                      if (double.tryParse(v) == null || double.parse(v) <= 0) {
                        return 'Giá không hợp lệ';
                      }
                      return null;
                    },
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: _Field(
                    ctrl: _durationCtrl,
                    label: 'Thời gian (giờ) *',
                    hint: '4',
                    keyboardType: TextInputType.number,
                    validator: (v) {
                      if (v!.isEmpty) return 'Nhập số giờ';
                      if (int.tryParse(v) == null || int.parse(v) <= 0) {
                        return 'Không hợp lệ';
                      }
                      return null;
                    },
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            _Field(
              ctrl: _maxGuestsCtrl,
              label: 'Số khách tối đa *',
              hint: '10',
              keyboardType: TextInputType.number,
              validator: (v) {
                if (v!.isEmpty) return 'Nhập số khách';
                if (int.tryParse(v) == null || int.parse(v) <= 0) {
                  return 'Không hợp lệ';
                }
                return null;
              },
            ),
            const SizedBox(height: 24),

            // Image picker section
            ImagePickerWidget(
              images: [
                // Include existing images if editing
                if (_isEdit && _currentTour != null) ..._currentTour!.images,
                // Include new selected images
                ..._selectedImages,
              ],
              onAddImages: _pickImages,
              onRemoveImage: (index) {
                // Calculate if this is an existing image or new image
                final existingImagesCount = _isEdit && _currentTour != null
                    ? _currentTour!.images.length
                    : 0;

                if (index >= existingImagesCount) {
                  // This is a new image, remove from _selectedImages
                  final newImageIndex = index - existingImagesCount;
                  _removeImage(newImageIndex);
                } else {
                  // This is an existing image, show warning
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text(
                        'Không thể xóa ảnh hiện có. Chỉ có thể thêm ảnh mới.',
                      ),
                      backgroundColor: Colors.orange,
                    ),
                  );
                }
              },
              maxImages: 5,
            ),

            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _submit,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFFE91E8C),
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  elevation: 0,
                ),
                child: _loading
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : Text(
                        _isEdit ? 'Lưu thay đổi' : 'Tạo tour',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
              ),
            ),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }
}

class _Field extends StatelessWidget {
  final TextEditingController ctrl;
  final String label;
  final String? hint;
  final int maxLines;
  final TextInputType? keyboardType;
  final String? Function(String?)? validator;

  const _Field({
    required this.ctrl,
    required this.label,
    this.hint,
    this.maxLines = 1,
    this.keyboardType,
    this.validator,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.bold,
            color: Colors.black54,
            letterSpacing: 0.5,
          ),
        ),
        const SizedBox(height: 6),
        TextFormField(
          controller: ctrl,
          maxLines: maxLines,
          keyboardType: keyboardType,
          validator: validator,
          decoration: InputDecoration(
            hintText: hint,
            filled: true,
            fillColor: Colors.grey.shade50,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: BorderSide(color: Colors.grey.shade300),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: BorderSide(color: Colors.grey.shade300),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(12),
              borderSide: const BorderSide(color: Color(0xFFE91E8C)),
            ),
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 14,
              vertical: 12,
            ),
          ),
        ),
      ],
    );
  }
}
