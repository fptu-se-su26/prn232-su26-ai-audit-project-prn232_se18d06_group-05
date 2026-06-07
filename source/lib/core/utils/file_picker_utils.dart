import 'dart:io';
import 'dart:typed_data';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter/material.dart';

/// Cross-platform file result - works on both web and mobile
class PickedFile {
  final String name;
  final int size;
  final Uint8List? bytes; // web: always set
  final File? file; // mobile/desktop: set when path available

  const PickedFile({
    required this.name,
    required this.size,
    this.bytes,
    this.file,
  });

  bool get hasBytes => bytes != null;
  bool get hasFile => file != null;
}

/// Utility class for file picking operations
class FilePickerUtils {
  static const int _maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

  /// Pick a PDF file — works on web AND mobile.
  ///
  /// Returns a [PickedFile] with either [bytes] (web) or [file] (mobile).
  static Future<PickedFile?> pickPDFFile(BuildContext context) async {
    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['pdf'],
        allowMultiple: false,
        // withData: true forces bytes to always be populated (works on web)
        withData: kIsWeb,
      );

      if (result == null || result.files.isEmpty) return null;

      final platformFile = result.files.single;

      // ── Validate extension ────────────────────────────────────────────────
      final name = platformFile.name;
      if (!name.toLowerCase().endsWith('.pdf')) {
        if (context.mounted) {
          _showError(context, 'Vui lòng chọn file PDF');
        }
        return null;
      }

      // ── Validate size ─────────────────────────────────────────────────────
      final size = platformFile.size;
      if (size > _maxFileSizeBytes) {
        if (context.mounted) {
          _showError(context, 'File quá lớn! Tối đa 10MB');
        }
        return null;
      }

      // ── Web: use bytes directly ───────────────────────────────────────────
      if (kIsWeb) {
        final bytes = platformFile.bytes;
        if (bytes == null) {
          if (context.mounted) _showError(context, 'Không thể đọc file');
          return null;
        }
        return PickedFile(name: name, size: size, bytes: bytes);
      }

      // ── Mobile/Desktop: use File path ─────────────────────────────────────
      final path = platformFile.path;
      if (path == null) {
        if (context.mounted)
          _showError(context, 'Không thể đọc đường dẫn file');
        return null;
      }
      return PickedFile(name: name, size: size, file: File(path));
    } catch (e) {
      if (context.mounted) {
        _showError(context, 'Lỗi khi chọn file: $e');
      }
      return null;
    }
  }

  static void _showError(BuildContext context, String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: Colors.red,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  static void showSuccess(BuildContext context, String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: Colors.green,
        behavior: SnackBarBehavior.floating,
      ),
    );
  }

  static String formatFileSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }
}
