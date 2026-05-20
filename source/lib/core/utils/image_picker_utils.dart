import 'package:flutter/material.dart';

/// Image picker utilities
/// This is a placeholder implementation until image_picker package is properly installed
class ImagePickerUtils {
  /// Pick multiple images
  static Future<List<String>> pickMultipleImages({
    int maxImages = 5,
    BuildContext? context,
  }) async {
    if (context == null) return [];

    // Show dialog to simulate image selection
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
      // Return sample image paths
      return [
        'https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800&h=600&fit=crop&crop=center',
        'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop&crop=center',
      ];
    }

    return [];
  }

  /// Pick single image
  static Future<String?> pickSingleImage({BuildContext? context}) async {
    final images = await pickMultipleImages(maxImages: 1, context: context);
    return images.isNotEmpty ? images.first : null;
  }
}

/// Image picker widget for displaying selected images
class ImagePickerWidget extends StatelessWidget {
  final List<String> images;
  final VoidCallback? onAddImages;
  final Function(int)? onRemoveImage;
  final int maxImages;
  final bool showExistingImages;

  const ImagePickerWidget({
    super.key,
    required this.images,
    this.onAddImages,
    this.onRemoveImage,
    this.maxImages = 5,
    this.showExistingImages = true,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Hình ảnh tour',
          style: TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.bold,
            color: Colors.black54,
            letterSpacing: 0.5,
          ),
        ),
        const SizedBox(height: 8),

        // Display images
        if (images.isNotEmpty) ...[
          SizedBox(
            height: 80,
            child: ListView.builder(
              scrollDirection: Axis.horizontal,
              itemCount: images.length,
              itemBuilder: (context, index) {
                return Container(
                  margin: const EdgeInsets.only(right: 8),
                  width: 80,
                  height: 80,
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.grey.shade300),
                  ),
                  child: Stack(
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: images[index].startsWith('http')
                            ? Image.network(
                                images[index],
                                width: 80,
                                height: 80,
                                fit: BoxFit.cover,
                                errorBuilder: (_, __, ___) => Container(
                                  color: Colors.grey.shade200,
                                  child: const Icon(Icons.broken_image),
                                ),
                              )
                            : Container(
                                width: 80,
                                height: 80,
                                color: Colors.grey.shade200,
                                child: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(
                                      Icons.image,
                                      size: 24,
                                      color: Colors.grey.shade600,
                                    ),
                                    const SizedBox(height: 4),
                                    Text(
                                      'Ảnh ${index + 1}',
                                      style: TextStyle(
                                        fontSize: 8,
                                        color: Colors.grey.shade600,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                      ),
                      if (onRemoveImage != null)
                        Positioned(
                          top: 4,
                          right: 4,
                          child: GestureDetector(
                            onTap: () => onRemoveImage!(index),
                            child: Container(
                              width: 20,
                              height: 20,
                              decoration: const BoxDecoration(
                                color: Colors.red,
                                shape: BoxShape.circle,
                              ),
                              child: const Icon(
                                Icons.close,
                                size: 12,
                                color: Colors.white,
                              ),
                            ),
                          ),
                        ),
                    ],
                  ),
                );
              },
            ),
          ),
          const SizedBox(height: 12),
        ],

        // Add images button
        OutlinedButton.icon(
          onPressed: images.length < maxImages ? onAddImages : null,
          icon: const Icon(Icons.add_photo_alternate),
          label: Text(
            images.isEmpty
                ? 'Thêm hình ảnh'
                : 'Thêm ảnh (${images.length}/$maxImages)',
          ),
          style: OutlinedButton.styleFrom(
            padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
          ),
        ),

        if (images.length >= maxImages)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(
              'Tối đa $maxImages hình ảnh',
              style: TextStyle(fontSize: 12, color: Colors.orange.shade700),
            ),
          ),
      ],
    );
  }
}

/// Additional utility methods for ImagePickerUtils
extension ImagePickerUtilsExtension on ImagePickerUtils {
  /// Upload image to storage service (placeholder)
  /// In production, implement actual upload to your storage service
  static Future<String> uploadImageToStorage(String imagePath) async {
    // TODO: Implement actual image upload
    // Example for Supabase Storage:
    // final file = File(imagePath);
    // final fileName = 'tour_images/${DateTime.now().millisecondsSinceEpoch}_${path.basename(imagePath)}';
    // final response = await Supabase.instance.client.storage
    //     .from('tour-images')
    //     .upload(fileName, file);
    // return Supabase.instance.client.storage
    //     .from('tour-images')
    //     .getPublicUrl(fileName);

    // Placeholder return
    return 'https://images.unsplash.com/photo-1555400038-63f5ba517a47?w=800&h=600&fit=crop&crop=center';
  }
}
