import 'dart:math' as math;
import 'package:flutter/material.dart';

/// Reusable loading indicator widget
class LoadingIndicator extends StatelessWidget {
  final String? message;
  final double size;

  const LoadingIndicator({super.key, this.message, this.size = 96.0});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          TripMateSpinner(size: size),
          if (message != null) ...[
            const SizedBox(height: 16),
            Text(
              message!,
              style: Theme.of(context).textTheme.bodyMedium,
              textAlign: TextAlign.center,
            ),
          ],
        ],
      ),
    );
  }
}

/// Full-screen overlay loading widget
class LoadingOverlay extends StatelessWidget {
  final bool isLoading;
  final Widget child;
  final String? message;

  const LoadingOverlay({
    super.key,
    required this.isLoading,
    required this.child,
    this.message,
  });

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        child,
        if (isLoading)
          Container(
            color: Colors.black.withOpacity(0.4),
            child: LoadingIndicator(message: message),
          ),
      ],
    );
  }
}

/// TripMate branded spinner — 4-ring animated SVG-style spinner
/// Replicates the CSS @keyframes ringA/B/C/D animation
class TripMateSpinner extends StatefulWidget {
  final double size;

  const TripMateSpinner({super.key, this.size = 96.0});

  @override
  State<TripMateSpinner> createState() => _TripMateSpinnerState();
}

class _TripMateSpinnerState extends State<TripMateSpinner>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 2),
    )..repeat();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: widget.size,
      height: widget.size,
      child: AnimatedBuilder(
        animation: _controller,
        builder: (context, _) {
          return CustomPaint(painter: _SpinnerPainter(_controller.value));
        },
      ),
    );
  }
}

class _SpinnerPainter extends CustomPainter {
  final double t; // 0..1

  _SpinnerPainter(this.t);

  // --- Keyframe interpolation helpers ---

  /// Interpolate a value between two keyframe stops
  double _lerp(double t, double t0, double t1, double v0, double v1) {
    if (t <= t0) return v0;
    if (t >= t1) return v1;
    final progress = (t - t0) / (t1 - t0);
    return v0 + (v1 - v0) * progress;
  }

  /// Given a list of [t, value] pairs, interpolate at time t
  double _interpolate(double t, List<List<double>> keyframes) {
    for (int i = 0; i < keyframes.length - 1; i++) {
      final t0 = keyframes[i][0];
      final t1 = keyframes[i + 1][0];
      if (t >= t0 && t <= t1) {
        return _lerp(t, t0, t1, keyframes[i][1], keyframes[i + 1][1]);
      }
    }
    return keyframes.last[1];
  }

  // ---- Ring A (r=105, circumference=660) ----
  // stroke: #f42f25
  _RingState _ringA(double t) {
    // dashArray (0..660 range, expressed as ratio 0..1)
    final dashFrames = [
      [0.00, 0.0],
      [0.04, 0.0],
      [0.12, 60 / 660],
      [0.32, 60 / 660],
      [0.40, 0.0],
      [0.54, 0.0],
      [0.62, 60 / 660],
      [0.82, 60 / 660],
      [0.90, 0.0],
      [1.00, 0.0],
    ];
    final widthFrames = [
      [0.00, 20.0],
      [0.04, 20.0],
      [0.12, 30.0],
      [0.32, 30.0],
      [0.40, 20.0],
      [0.54, 20.0],
      [0.62, 30.0],
      [0.82, 30.0],
      [0.90, 20.0],
      [1.00, 20.0],
    ];
    final offsetFrames = [
      [0.00, -330.0],
      [0.04, -330.0],
      [0.12, -335.0],
      [0.32, -595.0],
      [0.40, -660.0],
      [0.54, -660.0],
      [0.62, -665.0],
      [0.82, -925.0],
      [0.90, -990.0],
      [1.00, -990.0],
    ];
    return _RingState(
      dashRatio: _interpolate(t, dashFrames.map((e) => e).toList()),
      strokeWidth: _interpolate(t, widthFrames.map((e) => e).toList()),
      dashOffset: _interpolate(t, offsetFrames.map((e) => e).toList()),
      circumference: 660.0,
    );
  }

  // ---- Ring B (r=35, circumference=220) ----
  // stroke: #f49725
  _RingState _ringB(double t) {
    final dashFrames = [
      [0.00, 0.0],
      [0.12, 0.0],
      [0.20, 20 / 220],
      [0.40, 20 / 220],
      [0.48, 0.0],
      [0.62, 0.0],
      [0.70, 20 / 220],
      [0.90, 20 / 220],
      [0.98, 0.0],
      [1.00, 0.0],
    ];
    final widthFrames = [
      [0.00, 20.0],
      [0.12, 20.0],
      [0.20, 30.0],
      [0.40, 30.0],
      [0.48, 20.0],
      [0.62, 20.0],
      [0.70, 30.0],
      [0.90, 30.0],
      [0.98, 20.0],
      [1.00, 20.0],
    ];
    final offsetFrames = [
      [0.00, -110.0],
      [0.12, -110.0],
      [0.20, -115.0],
      [0.40, -195.0],
      [0.48, -220.0],
      [0.62, -220.0],
      [0.70, -225.0],
      [0.90, -305.0],
      [0.98, -330.0],
      [1.00, -330.0],
    ];
    return _RingState(
      dashRatio: _interpolate(t, dashFrames.map((e) => e).toList()),
      strokeWidth: _interpolate(t, widthFrames.map((e) => e).toList()),
      dashOffset: _interpolate(t, offsetFrames.map((e) => e).toList()),
      circumference: 220.0,
    );
  }

  // ---- Ring C (cx=85, r=70, circumference=440) ----
  // stroke: #255ff4
  _RingState _ringC(double t) {
    final dashFrames = [
      [0.00, 0.0],
      [0.08, 40 / 440],
      [0.28, 40 / 440],
      [0.36, 0.0],
      [0.58, 0.0],
      [0.66, 40 / 440],
      [0.86, 40 / 440],
      [0.94, 0.0],
      [1.00, 0.0],
    ];
    final widthFrames = [
      [0.00, 20.0],
      [0.08, 30.0],
      [0.28, 30.0],
      [0.36, 20.0],
      [0.58, 20.0],
      [0.66, 30.0],
      [0.86, 30.0],
      [0.94, 20.0],
      [1.00, 20.0],
    ];
    final offsetFrames = [
      [0.00, 0.0],
      [0.08, -5.0],
      [0.28, -175.0],
      [0.36, -220.0],
      [0.58, -220.0],
      [0.66, -225.0],
      [0.86, -395.0],
      [0.94, -440.0],
      [1.00, -440.0],
    ];
    return _RingState(
      dashRatio: _interpolate(t, dashFrames.map((e) => e).toList()),
      strokeWidth: _interpolate(t, widthFrames.map((e) => e).toList()),
      dashOffset: _interpolate(t, offsetFrames.map((e) => e).toList()),
      circumference: 440.0,
    );
  }

  // ---- Ring D (cx=155, r=70, circumference=440) ----
  // stroke: #f42582
  _RingState _ringD(double t) {
    final dashFrames = [
      [0.00, 0.0],
      [0.08, 0.0],
      [0.16, 40 / 440],
      [0.36, 40 / 440],
      [0.44, 0.0],
      [0.50, 0.0],
      [0.58, 40 / 440],
      [0.78, 40 / 440],
      [0.86, 0.0],
      [1.00, 0.0],
    ];
    final widthFrames = [
      [0.00, 20.0],
      [0.08, 20.0],
      [0.16, 30.0],
      [0.36, 30.0],
      [0.44, 20.0],
      [0.50, 20.0],
      [0.58, 30.0],
      [0.78, 30.0],
      [0.86, 20.0],
      [1.00, 20.0],
    ];
    final offsetFrames = [
      [0.00, 0.0],
      [0.08, 0.0],
      [0.16, -5.0],
      [0.36, -175.0],
      [0.44, -220.0],
      [0.50, -220.0],
      [0.58, -225.0],
      [0.78, -395.0],
      [0.86, -440.0],
      [1.00, -440.0],
    ];
    return _RingState(
      dashRatio: _interpolate(t, dashFrames.map((e) => e).toList()),
      strokeWidth: _interpolate(t, widthFrames.map((e) => e).toList()),
      dashOffset: _interpolate(t, offsetFrames.map((e) => e).toList()),
      circumference: 440.0,
    );
  }

  void _drawRing(
    Canvas canvas,
    Size size,
    _RingState state,
    Color color,
    Offset center,
    double radius,
  ) {
    // Scale from SVG 240x240 to actual widget size
    final scale = size.width / 240.0;
    final scaledRadius = radius * scale;
    final scaledStroke = state.strokeWidth * scale;
    final scaledCircumference = state.circumference * scale;
    final scaledDash = state.dashRatio * scaledCircumference;
    final scaledOffset = state.dashOffset * scale;

    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = scaledStroke
      ..strokeCap = StrokeCap.round;

    if (scaledDash > 0.5) {
      paint.strokeJoin = StrokeJoin.round;
    }

    final scaledCenter = Offset(center.dx * scale, center.dy * scale);
    final rect = Rect.fromCircle(center: scaledCenter, radius: scaledRadius);

    // Convert dashOffset to start angle
    // CSS stroke-dashoffset shifts the start of the dash pattern along the path
    // Negative offset means the dash starts further along the path
    final startAngle =
        -math.pi / 2 + (scaledOffset / scaledCircumference) * 2 * math.pi;

    if (scaledDash <= 0.5) {
      // No visible dash — draw nothing (gap fills entire circumference)
      return;
    }

    final sweepAngle = (scaledDash / scaledCircumference) * 2 * math.pi;

    canvas.drawArc(rect, startAngle, sweepAngle, false, paint);
  }

  @override
  void paint(Canvas canvas, Size size) {
    final a = _ringA(t);
    final b = _ringB(t);
    final c = _ringC(t);
    final d = _ringD(t);

    // Ring A: cx=120, cy=120, r=105
    _drawRing(
      canvas,
      size,
      a,
      const Color(0xFFF42F25),
      const Offset(120, 120),
      105,
    );

    // Ring B: cx=120, cy=120, r=35
    _drawRing(
      canvas,
      size,
      b,
      const Color(0xFFF49725),
      const Offset(120, 120),
      35,
    );

    // Ring C: cx=85, cy=120, r=70
    _drawRing(
      canvas,
      size,
      c,
      const Color(0xFF255FF4),
      const Offset(85, 120),
      70,
    );

    // Ring D: cx=155, cy=120, r=70
    _drawRing(
      canvas,
      size,
      d,
      const Color(0xFFF42582),
      const Offset(155, 120),
      70,
    );
  }

  @override
  bool shouldRepaint(_SpinnerPainter oldDelegate) => oldDelegate.t != t;
}

class _RingState {
  final double dashRatio; // dash length as fraction of circumference
  final double strokeWidth; // in SVG units (will be scaled)
  final double dashOffset; // in SVG units (negative = forward shift)
  final double circumference; // full circumference in SVG units

  const _RingState({
    required this.dashRatio,
    required this.strokeWidth,
    required this.dashOffset,
    required this.circumference,
  });
}
