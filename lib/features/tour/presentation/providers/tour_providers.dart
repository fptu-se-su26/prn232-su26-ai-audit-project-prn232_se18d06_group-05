import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/datasources/api_tour_datasource.dart';
import '../../data/datasources/tour_remote_datasource.dart';
import '../../data/repositories/tour_repository_impl.dart';
import '../../domain/repositories/tour_repository.dart';
import '../../domain/usecases/create_tour_usecase.dart';
import '../../domain/usecases/get_tour_by_id_usecase.dart';
import '../../domain/usecases/get_tours_usecase.dart';
import '../../domain/usecases/search_tours_usecase.dart';

/// ── Datasource switch ────────────────────────────────────────────────────────
/// true  → ASP.NET Web API
/// false → Supabase trực tiếp (legacy)
const bool _useWebApi = true;

final tourRemoteDataSourceProvider = Provider<TourRemoteDataSource>((ref) {
  if (_useWebApi) return ApiTourDataSource();
  return TourRemoteDataSourceImpl();
});

/// Tour repository provider
final tourRepositoryProvider = Provider<TourRepository>((ref) {
  final remoteDataSource = ref.watch(tourRemoteDataSourceProvider);
  return TourRepositoryImpl(remoteDataSource: remoteDataSource);
});

/// Get tours use case provider
final getToursUseCaseProvider = Provider<GetToursUseCase>((ref) {
  final repository = ref.watch(tourRepositoryProvider);
  return GetToursUseCase(repository);
});

/// Get tour by ID use case provider
final getTourByIdUseCaseProvider = Provider<GetTourByIdUseCase>((ref) {
  final repository = ref.watch(tourRepositoryProvider);
  return GetTourByIdUseCase(repository);
});

/// Create tour use case provider
final createTourUseCaseProvider = Provider<CreateTourUseCase>((ref) {
  final repository = ref.watch(tourRepositoryProvider);
  return CreateTourUseCase(repository);
});

/// Search tours use case provider
final searchToursUseCaseProvider = Provider<SearchToursUseCase>((ref) {
  final repository = ref.watch(tourRepositoryProvider);
  return SearchToursUseCase(repository);
});
