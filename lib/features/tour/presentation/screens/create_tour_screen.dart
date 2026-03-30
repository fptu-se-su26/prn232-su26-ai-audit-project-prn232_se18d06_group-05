import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/tour_providers.dart';

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

  bool get _isEdit => widget.tourId != null;

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
        );
      }
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              _isEdit ? 'Đã cập nhật tour' : 'Đã tạo tour thành công',
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
                      if (double.tryParse(v) == null || double.parse(v) <= 0)
                        return 'Giá không hợp lệ';
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
                      if (int.tryParse(v) == null || int.parse(v) <= 0)
                        return 'Không hợp lệ';
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
                if (int.tryParse(v) == null || int.parse(v) <= 0)
                  return 'Không hợp lệ';
                return null;
              },
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
