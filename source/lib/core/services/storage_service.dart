import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:supabase_flutter/supabase_flutter.dart';
import '../utils/file_picker_utils.dart';

/// Service for managing file uploads to Supabase Storage
class StorageService {
  final SupabaseClient _supabase = Supabase.instance.client;
  static const String _bucketName = 'guide-certificates';

  // ── Upload via PickedFile (cross-platform) ──────────────────────────────

  /// Upload a [PickedFile] (works on web + mobile).
  Future<String> uploadPickedCertificate({
    required PickedFile pickedFile,
    required String userId,
  }) async {
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    final filePath = '$userId/certificate_$timestamp.pdf';

    try {
      if (kIsWeb || pickedFile.hasBytes) {
        // Web — upload raw bytes
        await _supabase.storage
            .from(_bucketName)
            .uploadBinary(
              filePath,
              pickedFile.bytes!,
              fileOptions: const FileOptions(
                contentType: 'application/pdf',
                upsert: false,
              ),
            );
      } else if (pickedFile.hasFile) {
        // Mobile / Desktop — upload from File
        final bytes = await pickedFile.file!.readAsBytes();
        await _supabase.storage
            .from(_bucketName)
            .uploadBinary(
              filePath,
              bytes,
              fileOptions: const FileOptions(
                contentType: 'application/pdf',
                upsert: false,
              ),
            );
      } else {
        throw Exception('PickedFile has neither bytes nor a file path');
      }

      return _supabase.storage.from(_bucketName).getPublicUrl(filePath);
    } on StorageException catch (e) {
      throw Exception('Storage error: ${e.message}');
    }
  }

  // ── Legacy: upload via File (mobile only) ──────────────────────────────

  Future<String> uploadCertificate({
    required File file,
    required String userId,
  }) async {
    final bytes = await file.readAsBytes();
    return _uploadBytes(bytes: bytes, userId: userId);
  }

  // ── Upload raw bytes ────────────────────────────────────────────────────

  Future<String> _uploadBytes({
    required Uint8List bytes,
    required String userId,
  }) async {
    final timestamp = DateTime.now().millisecondsSinceEpoch;
    final filePath = '$userId/certificate_$timestamp.pdf';

    try {
      await _supabase.storage
          .from(_bucketName)
          .uploadBinary(
            filePath,
            bytes,
            fileOptions: const FileOptions(
              contentType: 'application/pdf',
              upsert: false,
            ),
          );

      return _supabase.storage.from(_bucketName).getPublicUrl(filePath);
    } on StorageException catch (e) {
      throw Exception('Storage error: ${e.message}');
    }
  }

  // ── Other helpers ───────────────────────────────────────────────────────

  Future<String> getSignedUrl({
    required String filePath,
    int expiresIn = 3600,
  }) async {
    try {
      return await _supabase.storage
          .from(_bucketName)
          .createSignedUrl(filePath, expiresIn);
    } on StorageException catch (e) {
      throw Exception('Storage error: ${e.message}');
    }
  }

  Future<void> deleteCertificate({required String filePath}) async {
    try {
      await _supabase.storage.from(_bucketName).remove([filePath]);
    } on StorageException catch (e) {
      throw Exception('Storage error: ${e.message}');
    }
  }

  Future<List<int>> downloadCertificate({required String filePath}) async {
    try {
      return await _supabase.storage.from(_bucketName).download(filePath);
    } on StorageException catch (e) {
      throw Exception('Storage error: ${e.message}');
    }
  }

  String formatFileSize(int bytes) {
    if (bytes < 1024) return '$bytes B';
    if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
    return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
  }
}
