import 'package:flutter/material.dart';
import '../../core/utils/file_picker_utils.dart';

/// Widget for uploading PDF certificate files
class CertificateUploadWidget extends StatefulWidget {
  final PickedFile? initialFile;
  final Function(PickedFile?) onFileChanged;
  final bool enabled;

  const CertificateUploadWidget({
    super.key,
    this.initialFile,
    required this.onFileChanged,
    this.enabled = true,
  });

  @override
  State<CertificateUploadWidget> createState() =>
      _CertificateUploadWidgetState();
}

class _CertificateUploadWidgetState extends State<CertificateUploadWidget> {
  PickedFile? _selectedFile;

  @override
  void initState() {
    super.initState();
    _selectedFile = widget.initialFile;
  }

  Future<void> _pickFile() async {
    if (!widget.enabled) return;

    final file = await FilePickerUtils.pickPDFFile(context);
    if (file != null) {
      setState(() {
        _selectedFile = file;
      });
      widget.onFileChanged(file);
    }
  }

  void _removeFile() {
    setState(() {
      _selectedFile = null;
    });
    widget.onFileChanged(null);
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'CHỨNG CHỈ HƯỚNG DẪN VIÊN',
          style: Theme.of(
            context,
          ).textTheme.labelSmall?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 8),

        // Upload area or file preview
        if (_selectedFile == null) _buildUploadArea() else _buildFilePreview(),
      ],
    );
  }

  Widget _buildUploadArea() {
    return InkWell(
      onTap: widget.enabled ? _pickFile : null,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          border: Border.all(color: Theme.of(context).dividerColor, width: 2),
          borderRadius: BorderRadius.circular(12),
          color: widget.enabled
              ? Colors.transparent
              : Theme.of(context).disabledColor.withValues(alpha: 0.1),
        ),
        child: Column(
          children: [
            Icon(
              Icons.cloud_upload_outlined,
              size: 48,
              color: widget.enabled
                  ? Theme.of(context).colorScheme.primary
                  : Theme.of(context).disabledColor,
            ),
            const SizedBox(height: 12),
            Text(
              'Nhấp để tải lên chứng chỉ',
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: widget.enabled ? null : Theme.of(context).disabledColor,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              'PDF, tối đa 10MB',
              style: TextStyle(fontSize: 12, color: Colors.grey),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildFilePreview() {
    final file = _selectedFile!;
    // Dùng file.name trực tiếp từ PickedFile (không cần getFileName)
    final fileName = file.name;
    final fileSizeText = FilePickerUtils.formatFileSize(file.size);

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.primaryContainer,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          Row(
            children: [
              const Icon(Icons.picture_as_pdf, color: Colors.red, size: 32),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      fileName,
                      style: const TextStyle(fontWeight: FontWeight.bold),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    Text(
                      fileSizeText,
                      style: const TextStyle(
                        fontSize: 12,
                        color: Colors.grey,
                      ),
                    ),
                  ],
                ),
              ),
              if (widget.enabled)
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: _removeFile,
                  tooltip: 'Xóa file',
                ),
            ],
          ),
          const SizedBox(height: 12),
          // PDF preview placeholder
          Container(
            height: 120,
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: Theme.of(context).dividerColor,
                width: 2,
                style: BorderStyle.solid,
              ),
            ),
            child: const Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.description, size: 40, color: Colors.grey),
                  SizedBox(height: 8),
                  Text(
                    'PDF Preview',
                    style: TextStyle(fontSize: 12, color: Colors.grey),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
