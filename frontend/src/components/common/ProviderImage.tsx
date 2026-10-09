import React, { useState, useEffect } from 'react';
import {
  Sparkles,
  Scissors,
  Package,
  Camera,
  Image as ImageIcon,
  Heart,
  Dumbbell,
  Briefcase,
  Activity,
  X,
  ChevronLeft,
  ChevronRight,
  Maximize2,
} from 'lucide-react';

export type ImageCategoryType =
  | 'logo'
  | 'cover'
  | 'service'
  | 'product'
  | 'gallery'
  | 'avatar';

export interface ProviderImageProps {
  src?: string | null;
  alt: string;
  type?: ImageCategoryType;
  category?: string;
  className?: string;
  aspectRatio?: 'square' | 'video' | 'cover' | 'auto';
  loading?: 'lazy' | 'eager';
  fallbackSrc?: string;
  onClick?: () => void;
}

/**
 * Section 105: PROVIDER IMAGE SYSTEM
 * - Supports logo, cover, service, product, gallery images
 * - Image fallbacks with curated aesthetic placeholders
 * - Zero broken-image icons: suppresses browser missing-image glyph
 * - Lazy-loads images with native loading="lazy" & decoding="async"
 */
export const ProviderImage: React.FC<ProviderImageProps> = ({
  src,
  alt,
  type = 'service',
  category,
  className = '',
  aspectRatio = 'auto',
  loading = 'lazy',
  fallbackSrc,
  onClick,
}) => {
  const [hasError, setHasError] = useState(false);
  const [isLoaded, setIsLoaded] = useState(false);

  // Reset state if src changes
  useEffect(() => {
    setHasError(false);
    setIsLoaded(false);
  }, [src]);

  const effectiveSrc = hasError && fallbackSrc ? fallbackSrc : src;
  const showFallback = !effectiveSrc || (hasError && !fallbackSrc);

  const getAspectClass = () => {
    switch (aspectRatio) {
      case 'square':
        return 'aspect-square';
      case 'video':
        return 'aspect-video';
      case 'cover':
        return 'aspect-[21/9] sm:aspect-[3/1]';
      case 'auto':
      default:
        return '';
    }
  };

  // Curated category icon for service/provider fallbacks
  const getCategoryIcon = () => {
    const cat = (category || alt || '').toLowerCase();
    if (cat.includes('hair') || cat.includes('barber') || cat.includes('salon')) {
      return <Scissors className="w-6 h-6 text-[#E8546A]" />;
    }
    if (cat.includes('dental') || cat.includes('health') || cat.includes('clinic')) {
      return <Activity className="w-6 h-6 text-blue-400" />;
    }
    if (cat.includes('fitness') || cat.includes('gym') || cat.includes('training')) {
      return <Dumbbell className="w-6 h-6 text-emerald-400" />;
    }
    if (cat.includes('photo') || cat.includes('media') || cat.includes('camera')) {
      return <Camera className="w-6 h-6 text-amber-400" />;
    }
    if (cat.includes('legal') || cat.includes('advisory') || cat.includes('business')) {
      return <Briefcase className="w-6 h-6 text-indigo-400" />;
    }
    if (cat.includes('spa') || cat.includes('massage') || cat.includes('wellness')) {
      return <Heart className="w-6 h-6 text-pink-400" />;
    }
    return <Sparkles className="w-6 h-6 text-[#E8546A]" />;
  };

  // Render stylized CSS fallback placeholder
  const renderFallback = () => {
    switch (type) {
      case 'logo':
      case 'avatar': {
        const initials = alt
          .split(' ')
          .map((n) => n[0])
          .join('')
          .substring(0, 2)
          .toUpperCase() || 'BL';
        return (
          <div
            className={`w-full h-full min-h-[44px] flex items-center justify-center bg-gradient-to-br from-[#181D2C] to-[#212638] border border-[#273142] text-white font-heading font-bold select-none shadow-inner ${className}`}
            onClick={onClick}
          >
            <span className="text-[#E8546A] tracking-wider">{initials}</span>
          </div>
        );
      }

      case 'cover':
        return (
          <div
            className={`w-full h-full relative overflow-hidden bg-gradient-to-br from-[#111620] via-[#1A2130] to-[#0D1117] flex items-center justify-center ${className}`}
            onClick={onClick}
          >
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_30%_30%,rgba(232,84,106,0.12),transparent_70%)]" />
            <div className="relative text-center p-6 space-y-2 select-none opacity-80">
              <div className="w-12 h-12 rounded-2xl bg-[#181D2C]/80 border border-[#273142] flex items-center justify-center mx-auto text-[#E8546A] shadow-lg">
                {getCategoryIcon()}
              </div>
              <p className="text-xs font-semibold text-[#8F9AAF] uppercase tracking-wider">{alt}</p>
            </div>
          </div>
        );

      case 'product':
        return (
          <div
            className={`w-full h-full flex flex-col items-center justify-center bg-[#151B27] border border-[#273142] p-4 text-center select-none ${className}`}
            onClick={onClick}
          >
            <div className="w-10 h-10 rounded-xl bg-amber-500/10 border border-amber-500/20 text-amber-400 flex items-center justify-center mb-1">
              <Package className="w-5 h-5" />
            </div>
            <span className="text-[10px] text-[#8F9AAF] truncate max-w-full font-medium">{alt}</span>
          </div>
        );

      case 'gallery':
        return (
          <div
            className={`w-full h-full flex flex-col items-center justify-center bg-[#151B27] border border-[#273142] p-4 text-center select-none ${className}`}
            onClick={onClick}
          >
            <div className="w-10 h-10 rounded-xl bg-purple-500/10 border border-purple-500/20 text-purple-400 flex items-center justify-center mb-1">
              <Camera className="w-5 h-5" />
            </div>
            <span className="text-[10px] text-[#8F9AAF] truncate max-w-full font-medium">Gallery Item</span>
          </div>
        );

      case 'service':
      default:
        return (
          <div
            className={`w-full h-full flex flex-col items-center justify-center bg-[#151B27] border border-[#273142] p-4 text-center select-none ${className}`}
            onClick={onClick}
          >
            <div className="w-10 h-10 rounded-xl bg-[#E8546A]/10 border border-[#E8546A]/20 text-[#E8546A] flex items-center justify-center mb-1">
              {getCategoryIcon()}
            </div>
            <span className="text-[10px] text-[#8F9AAF] truncate max-w-full font-medium">{alt}</span>
          </div>
        );
    }
  };

  if (showFallback) {
    return <div className={`relative overflow-hidden ${getAspectClass()}`}>{renderFallback()}</div>;
  }

  return (
    <div
      className={`relative overflow-hidden ${getAspectClass()} ${className}`}
      onClick={onClick}
    >
      {/* Shimmer placeholder while loading */}
      {!isLoaded && !hasError && (
        <div className="absolute inset-0 bg-[#1A2130] animate-pulse flex items-center justify-center">
          <ImageIcon className="w-5 h-5 text-[#273142]" />
        </div>
      )}

      <img
        src={effectiveSrc!}
        alt={alt}
        loading={loading}
        decoding="async"
        onLoad={() => setIsLoaded(true)}
        onError={() => {
          // Immediately set error flag without rendering broken icon
          setHasError(true);
        }}
        className={`w-full h-full object-cover transition-opacity duration-300 ${
          isLoaded ? 'opacity-100' : 'opacity-0'
        }`}
      />
    </div>
  );
};

export interface ProviderGalleryProps {
  images?: string[];
  providerName?: string;
  category?: string;
  onImageClick?: (index: number) => void;
}

/**
 * Section 105: Gallery subsystem with lightbox preview modal
 */
export const ProviderGallery: React.FC<ProviderGalleryProps> = ({
  images = [],
  providerName = 'Venue',
  category,
}) => {
  const [selectedImageIndex, setSelectedImageIndex] = useState<number | null>(null);

  // Fallback gallery images if none provided
  const displayImages =
    images.length > 0
      ? images
      : [
          'https://images.unsplash.com/photo-1560066984-138dadb4c035?auto=format&fit=crop&w=800&q=80',
          'https://images.unsplash.com/photo-1540555700478-4be289fbecef?auto=format&fit=crop&w=800&q=80',
          'https://images.unsplash.com/photo-1600334089648-b0d9d3028eb2?auto=format&fit=crop&w=800&q=80',
          'https://images.unsplash.com/photo-1519823551278-64ac92734fb1?auto=format&fit=crop&w=800&q=80',
        ];

  const handlePrev = () => {
    if (selectedImageIndex === null) return;
    setSelectedImageIndex((selectedImageIndex - 1 + displayImages.length) % displayImages.length);
  };

  const handleNext = () => {
    if (selectedImageIndex === null) return;
    setSelectedImageIndex((selectedImageIndex + 1) % displayImages.length);
  };

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="font-heading text-lg font-bold text-white">Venue &amp; Service Gallery</h3>
          <p className="text-xs text-[#7E88A8]">Visual tour of our studio, treatment rooms, and atmosphere</p>
        </div>
        <span className="text-xs text-[#7E88A8] bg-[#181D2C] px-3 py-1 rounded-full border border-[#212638]">
          {displayImages.length} Photos
        </span>
      </div>

      {/* Gallery Grid */}
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-3">
        {displayImages.map((imgUrl, idx) => (
          <div
            key={idx}
            onClick={() => setSelectedImageIndex(idx)}
            className="group relative rounded-2xl overflow-hidden cursor-pointer aspect-square bg-[#181D2C] border border-[#212638] hover:border-[#E8546A] transition-all"
          >
            <ProviderImage
              src={imgUrl}
              alt={`${providerName} Photo ${idx + 1}`}
              type="gallery"
              category={category}
              className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
            />
            <div className="absolute inset-0 bg-[#0A0C13]/40 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center">
              <Maximize2 className="w-5 h-5 text-white" />
            </div>
          </div>
        ))}
      </div>

      {/* Lightbox Modal */}
      {selectedImageIndex !== null && (
        <div className="fixed inset-0 z-50 bg-black/90 backdrop-blur-md flex items-center justify-center p-4">
          <button
            onClick={() => setSelectedImageIndex(null)}
            className="absolute top-5 right-5 p-3 rounded-full bg-white/10 hover:bg-white/20 text-white transition-colors"
            title="Close Lightbox"
          >
            <X className="w-6 h-6" />
          </button>

          <button
            onClick={handlePrev}
            className="absolute left-4 p-3 rounded-full bg-white/10 hover:bg-white/20 text-white transition-colors"
            title="Previous Photo"
          >
            <ChevronLeft className="w-6 h-6" />
          </button>

          <div className="max-w-4xl max-h-[85vh] rounded-2xl overflow-hidden border border-[#212638] bg-[#111520] shadow-2xl">
            <ProviderImage
              src={displayImages[selectedImageIndex]}
              alt={`${providerName} Full Preview`}
              type="gallery"
              category={category}
              className="w-full max-h-[75vh] object-contain"
              loading="eager"
            />
            <div className="p-4 bg-[#181D2C] flex items-center justify-between text-xs text-[#7E88A8]">
              <span className="font-semibold text-white">{providerName} &bull; Image {selectedImageIndex + 1} of {displayImages.length}</span>
              <span>Tap arrows or click outside to dismiss</span>
            </div>
          </div>

          <button
            onClick={handleNext}
            className="absolute right-4 p-3 rounded-full bg-white/10 hover:bg-white/20 text-white transition-colors"
            title="Next Photo"
          >
            <ChevronRight className="w-6 h-6" />
          </button>
        </div>
      )}
    </div>
  );
};
