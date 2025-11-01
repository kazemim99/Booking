import { useState } from "react";
import { Button } from "../ui/button";
import { ProgressIndicator } from "./ProgressIndicator";
import { Upload, X, Star } from "lucide-react";

interface GalleryImage {
  id: string;
  url: string;
  isPrimary: boolean;
}

interface GalleryProps {
  onNext: (data: any) => void;
  onBack: () => void;
}

export function Gallery({ onNext, onBack }: GalleryProps) {
  const [images, setImages] = useState<GalleryImage[]>([]);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files) return;

    Array.from(files).forEach((file) => {
      const reader = new FileReader();
      reader.onloadend = () => {
        setImages((prev) => [
          ...prev,
          {
            id: Date.now().toString() + Math.random(),
            url: reader.result as string,
            isPrimary: prev.length === 0, // First image is primary by default
          },
        ]);
      };
      reader.readAsDataURL(file);
    });
  };

  const handleSetPrimary = (id: string) => {
    setImages(images.map((img) => ({
      ...img,
      isPrimary: img.id === id,
    })));
  };

  const handleRemove = (id: string) => {
    const filtered = images.filter((img) => img.id !== id);
    // If removed image was primary and there are other images, make the first one primary
    if (filtered.length > 0 && !filtered.some(img => img.isPrimary)) {
      filtered[0].isPrimary = true;
    }
    setImages(filtered);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onNext({ gallery: images });
  };

  return (
    <div className="min-h-screen bg-background p-4 py-8">
      <div className="max-w-2xl mx-auto">
        <ProgressIndicator currentStep={7} totalSteps={8} />

        <div className="bg-card rounded-2xl shadow-sm p-8 border">
          <div className="mb-6">
            <h2 className="mb-2">گالری تصاویر</h2>
            <p className="text-muted-foreground">
              تصاویری از کسب‌و‌کار خود آپلود کنید (اختیاری)
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Upload Area */}
            <label className="cursor-pointer block">
              <div className="border-2 border-dashed border-border rounded-xl p-8 hover:border-primary transition-colors text-center">
                <Upload className="w-12 h-12 mx-auto mb-3 text-muted-foreground" />
                <h3 className="mb-1">آپلود تصاویر</h3>
                <p className="text-sm text-muted-foreground">
                  کلیک کنید یا فایل‌ها را اینجا بکشید
                </p>
                <p className="text-xs text-muted-foreground mt-2">
                  می‌توانید چند تصویر همزمان انتخاب کنید
                </p>
              </div>
              <input
                type="file"
                accept="image/*"
                multiple
                onChange={handleFileChange}
                className="hidden"
              />
            </label>

            {/* Image Grid */}
            {images.length > 0 && (
              <div>
                <Label className="mb-3 block">
                  تصاویر آپلود شده ({images.length})
                </Label>
                <div className="grid grid-cols-2 sm:grid-cols-3 gap-4">
                  {images.map((image) => (
                    <div
                      key={image.id}
                      className="relative group aspect-square rounded-lg overflow-hidden border-2 transition-all"
                      style={{
                        borderColor: image.isPrimary ? "var(--primary)" : "var(--border)",
                      }}
                    >
                      <img
                        src={image.url}
                        alt="Gallery"
                        className="w-full h-full object-cover"
                      />
                      
                      {/* Primary Badge */}
                      {image.isPrimary && (
                        <div className="absolute top-2 right-2 bg-primary text-primary-foreground px-2 py-1 rounded-md text-xs flex items-center gap-1">
                          <Star className="w-3 h-3 fill-current" />
                          اصلی
                        </div>
                      )}

                      {/* Overlay Controls */}
                      <div className="absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                        {!image.isPrimary && (
                          <Button
                            type="button"
                            size="sm"
                            variant="secondary"
                            onClick={() => handleSetPrimary(image.id)}
                          >
                            <Star className="w-4 h-4 ml-1" />
                            تنظیم به عنوان اصلی
                          </Button>
                        )}
                        <Button
                          type="button"
                          size="icon"
                          variant="destructive"
                          onClick={() => handleRemove(image.id)}
                        >
                          <X className="w-4 h-4" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            <div className="flex gap-3">
              <Button type="button" variant="outline" onClick={onBack} className="flex-1">
                قبلی
              </Button>
              <Button type="submit" className="flex-1">
                بعدی
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

function Label({ children, className, ...props }: React.LabelHTMLAttributes<HTMLLabelElement>) {
  return (
    <label className={className} {...props}>
      {children}
    </label>
  );
}
