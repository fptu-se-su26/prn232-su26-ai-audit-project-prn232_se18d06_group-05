import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/constants/app_constants.dart';
import '../../../../core/utils/file_picker_utils.dart';
import '../../../../shared/widgets/certificate_upload_widget.dart';
import '../providers/auth_state_provider.dart';
import '../../../../main.dart' show AuthWrapper;

/// Enhanced SignUp Screen with role selection and guide-specific fields
class SignUpWithRoleScreen extends ConsumerStatefulWidget {
  const SignUpWithRoleScreen({super.key});

  @override
  ConsumerState<SignUpWithRoleScreen> createState() =>
      _SignUpWithRoleScreenState();
}

class _SignUpWithRoleScreenState extends ConsumerState<SignUpWithRoleScreen> {
  final _formKey = GlobalKey<FormState>();
  final _fullNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _phoneController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  // Guide-specific controllers
  final _bioController = TextEditingController();
  final _languagesController = TextEditingController();

  bool _obscurePassword = true;
  bool _obscureConfirmPassword = true;

  // Role selection
  String _selectedRole = 'traveler'; // 'traveler' or 'guide'

  // Guide-specific fields
  String? _selectedExperience;
  String? _selectedSpecialization;
  PickedFile? _certificateFile;

  @override
  void dispose() {
    _fullNameController.dispose();
    _emailController.dispose();
    _phoneController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    _bioController.dispose();
    _languagesController.dispose();
    super.dispose();
  }

  Future<void> _handleSignUp() async {
    if (!_formKey.currentState!.validate()) return;

    // Additional validation for guide role
    if (_selectedRole == 'guide') {
      if (_selectedExperience == null) {
        _showError('Vui lòng chọn kinh nghiệm');
        return;
      }
      if (_selectedSpecialization == null) {
        _showError('Vui lòng chọn chuyên môn');
        return;
      }
    }

    bool success;

    if (_selectedRole == 'traveler') {
      // Sign up as traveler
      success = await ref
          .read(authStateProvider.notifier)
          .signUp(
            email: _emailController.text.trim(),
            password: _passwordController.text,
            fullName: _fullNameController.text.trim(),
            phoneNumber: _phoneController.text.trim().replaceAll(RegExp(r'[^0-9]'), ''),
          );
    } else {
      // Sign up as guide
      success = await ref
          .read(authStateProvider.notifier)
          .signUpGuide(
            email: _emailController.text.trim(),
            password: _passwordController.text,
            fullName: _fullNameController.text.trim(),
            phoneNumber: _phoneController.text.trim().replaceAll(RegExp(r'[^0-9]'), ''),
            experience: _selectedExperience,
            specialization: _selectedSpecialization,
            languages: _languagesController.text.trim(),
            bio: _bioController.text.trim(),
            certificatePickedFile: _certificateFile,
          );
    }

    if (success && mounted) {
      final message = _selectedRole == 'guide'
          ? 'Đăng ký thành công! Vui lòng chờ admin phê duyệt.'
          : 'Đăng ký thành công!';

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(message), backgroundColor: Colors.green),
      );

      Navigator.of(context).pushAndRemoveUntil(
        MaterialPageRoute(builder: (_) => const AuthWrapper()),
        (_) => false,
      );
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), backgroundColor: Colors.red),
    );
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authStateProvider);

    // Show error if any
    ref.listen<AuthState>(authStateProvider, (previous, next) {
      if (next.error != null) {
        _showError(next.error!);
        ref.read(authStateProvider.notifier).clearError();
      }
    });

    return Scaffold(
      appBar: AppBar(title: const Text('Đăng ký')),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24.0),
            child: Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Logo
                  Icon(
                    Icons.person_add_outlined,
                    size: 80,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                  const SizedBox(height: 16),

                  // Title
                  Text(
                    'Tạo tài khoản mới',
                    style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 32),

                  // Role selection
                  _buildRoleSelection(),
                  const SizedBox(height: 24),

                  // Common fields
                  ..._buildCommonFields(),

                  // Guide-specific fields
                  if (_selectedRole == 'guide') ...[
                    const SizedBox(height: 24),
                    const Divider(),
                    const SizedBox(height: 16),
                    Text(
                      'Thông tin hướng dẫn viên',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 16),
                    ..._buildGuideFields(),
                  ],

                  const SizedBox(height: 24),

                  // Sign up button
                  ElevatedButton(
                    onPressed: authState.isLoading ? null : _handleSignUp,
                    style: ElevatedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 16),
                    ),
                    child: authState.isLoading
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Text('Đăng ký'),
                  ),
                  const SizedBox(height: 16),

                  // Login link
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Text('Đã có tài khoản? '),
                      TextButton(
                        onPressed: () {
                          Navigator.of(context).pop();
                        },
                        child: const Text('Đăng nhập'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildRoleSelection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'VAI TRÒ',
          style: Theme.of(
            context,
          ).textTheme.labelSmall?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: _buildRoleCard(
                role: 'traveler',
                icon: Icons.luggage,
                label: 'Du khách',
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _buildRoleCard(
                role: 'guide',
                icon: Icons.badge,
                label: 'Hướng dẫn viên',
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildRoleCard({
    required String role,
    required IconData icon,
    required String label,
  }) {
    final isSelected = _selectedRole == role;

    return InkWell(
      onTap: () {
        setState(() {
          _selectedRole = role;
        });
      },
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          border: Border.all(
            color: isSelected
                ? Theme.of(context).colorScheme.primary
                : Theme.of(context).dividerColor,
            width: 2,
          ),
          borderRadius: BorderRadius.circular(12),
          color: isSelected
              ? Theme.of(context).colorScheme.primaryContainer
              : Colors.transparent,
        ),
        child: Column(
          children: [
            Icon(
              icon,
              size: 32,
              color: isSelected
                  ? Theme.of(context).colorScheme.primary
                  : Colors.grey,
            ),
            const SizedBox(height: 8),
            Text(
              label,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: isSelected ? null : Colors.grey,
              ),
            ),
          ],
        ),
      ),
    );
  }

  List<Widget> _buildCommonFields() {
    return [
      // Full name field
      TextFormField(
        controller: _fullNameController,
        decoration: const InputDecoration(
          labelText: 'Họ và tên',
          prefixIcon: Icon(Icons.person_outlined),
        ),
        validator: (value) {
          if (value == null || value.isEmpty) {
            return 'Vui lòng nhập họ tên';
          }
          return null;
        },
      ),
      const SizedBox(height: 16),

      // Email field
      TextFormField(
        controller: _emailController,
        keyboardType: TextInputType.emailAddress,
        decoration: const InputDecoration(
          labelText: 'Email',
          prefixIcon: Icon(Icons.email_outlined),
        ),
        validator: (value) {
          if (value == null || value.isEmpty) {
            return AppConstants.emailRequired;
          }
          if (!value.contains('@')) {
            return AppConstants.emailInvalid;
          }
          return null;
        },
      ),
      const SizedBox(height: 16),

      // Phone field (required for guide)
      TextFormField(
        controller: _phoneController,
        keyboardType: TextInputType.phone,
        decoration: const InputDecoration(
          labelText: 'Số điện thoại',
          prefixIcon: Icon(Icons.phone_outlined),
        ),
        validator: (value) {
          if (value == null || value.trim().isEmpty) {
            return 'Vui lòng nhập số điện thoại';
          }
          // Chỉ giữ lại chữ số để kiểm tra
          final digits = value.trim().replaceAll(RegExp(r'[^0-9]'), '');
          if (digits.length < 10 || digits.length > 11) {
            return 'Số điện thoại phải có 10-11 chữ số';
          }
          return null;
        },
      ),
      const SizedBox(height: 16),

      // Password field
      TextFormField(
        controller: _passwordController,
        obscureText: _obscurePassword,
        decoration: InputDecoration(
          labelText: 'Mật khẩu',
          prefixIcon: const Icon(Icons.lock_outlined),
          suffixIcon: IconButton(
            icon: Icon(
              _obscurePassword
                  ? Icons.visibility_outlined
                  : Icons.visibility_off_outlined,
            ),
            onPressed: () {
              setState(() {
                _obscurePassword = !_obscurePassword;
              });
            },
          ),
        ),
        validator: (value) {
          if (value == null || value.isEmpty) {
            return AppConstants.passwordRequired;
          }
          if (value.length < 6) {
            return 'Mật khẩu tối thiểu 6 ký tự';
          }
          return null;
        },
      ),
      const SizedBox(height: 16),

      // Confirm password field
      TextFormField(
        controller: _confirmPasswordController,
        obscureText: _obscureConfirmPassword,
        decoration: InputDecoration(
          labelText: 'Xác nhận mật khẩu',
          prefixIcon: const Icon(Icons.lock_outlined),
          suffixIcon: IconButton(
            icon: Icon(
              _obscureConfirmPassword
                  ? Icons.visibility_outlined
                  : Icons.visibility_off_outlined,
            ),
            onPressed: () {
              setState(() {
                _obscureConfirmPassword = !_obscureConfirmPassword;
              });
            },
          ),
        ),
        validator: (value) {
          if (value == null || value.isEmpty) {
            return 'Vui lòng xác nhận mật khẩu';
          }
          if (value != _passwordController.text) {
            return 'Mật khẩu không khớp';
          }
          return null;
        },
      ),
    ];
  }

  List<Widget> _buildGuideFields() {
    return [
      // Experience dropdown
      DropdownButtonFormField<String>(
        initialValue: _selectedExperience,
        decoration: const InputDecoration(
          labelText: 'Kinh nghiệm (năm)',
          prefixIcon: Icon(Icons.work_history_outlined),
        ),
        items: const [
          DropdownMenuItem(value: '0-1', child: Text('Dưới 1 năm')),
          DropdownMenuItem(value: '1-3', child: Text('1-3 năm')),
          DropdownMenuItem(value: '3-5', child: Text('3-5 năm')),
          DropdownMenuItem(value: '5-10', child: Text('5-10 năm')),
          DropdownMenuItem(value: '10+', child: Text('Trên 10 năm')),
        ],
        onChanged: (value) {
          setState(() {
            _selectedExperience = value;
          });
        },
      ),
      const SizedBox(height: 16),

      // Specialization dropdown
      DropdownButtonFormField<String>(
        initialValue: _selectedSpecialization,
        decoration: const InputDecoration(
          labelText: 'Chuyên môn',
          prefixIcon: Icon(Icons.category_outlined),
        ),
        items: const [
          DropdownMenuItem(value: 'cultural', child: Text('Du lịch văn hóa')),
          DropdownMenuItem(value: 'adventure', child: Text('Du lịch mạo hiểm')),
          DropdownMenuItem(value: 'nature', child: Text('Du lịch sinh thái')),
          DropdownMenuItem(value: 'food', child: Text('Du lịch ẩm thực')),
          DropdownMenuItem(value: 'historical', child: Text('Du lịch lịch sử')),
          DropdownMenuItem(value: 'beach', child: Text('Du lịch biển')),
          DropdownMenuItem(value: 'mountain', child: Text('Du lịch núi')),
        ],
        onChanged: (value) {
          setState(() {
            _selectedSpecialization = value;
          });
        },
      ),
      const SizedBox(height: 16),

      // Languages field
      TextFormField(
        controller: _languagesController,
        decoration: const InputDecoration(
          labelText: 'Ngôn ngữ',
          prefixIcon: Icon(Icons.translate_outlined),
          hintText: 'Tiếng Việt, English, 中文',
        ),
      ),
      const SizedBox(height: 16),

      // Certificate upload
      CertificateUploadWidget(
        initialFile: _certificateFile,
        onFileChanged: (file) {
          setState(() {
            _certificateFile = file;
          });
        },
      ),
      const SizedBox(height: 16),

      // Bio field
      TextFormField(
        controller: _bioController,
        maxLines: 4,
        maxLength: 500,
        decoration: const InputDecoration(
          labelText: 'Giới thiệu bản thân',
          alignLabelWithHint: true,
          hintText: 'Chia sẻ về kinh nghiệm và đam mê của bạn...',
        ),
      ),
      const SizedBox(height: 16),

      // Info note
      Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: Colors.blue.shade50,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: Colors.blue.shade200),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.info_outline, color: Colors.blue.shade700, size: 20),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                'Tài khoản của bạn sẽ được xem xét và xác minh trong vòng 24-48 giờ.',
                style: TextStyle(fontSize: 12, color: Colors.blue.shade700),
              ),
            ),
          ],
        ),
      ),
    ];
  }
}
