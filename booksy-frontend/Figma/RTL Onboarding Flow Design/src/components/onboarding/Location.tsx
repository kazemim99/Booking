import { useState } from "react";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { ProgressIndicator } from "./ProgressIndicator";
import { MapPin } from "lucide-react";

interface LocationProps {
  onNext: (data: any) => void;
  onBack: () => void;
}

export function Location({ onNext, onBack }: LocationProps) {
  const [formData, setFormData] = useState({
    address: "",
    city: "",
    province: "",
    postalCode: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const validate = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.address) {
      newErrors.address = "آدرس الزامی است";
    }
    if (!formData.city) {
      newErrors.city = "شهر الزامی است";
    }
    if (!formData.province) {
      newErrors.province = "استان الزامی است";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validate()) {
      onNext(formData);
    }
  };

  return (
    <div className="min-h-screen bg-background p-4 py-8">
      <div className="max-w-2xl mx-auto">
        <ProgressIndicator currentStep={3} totalSteps={8} />

        <div className="bg-card rounded-2xl shadow-sm p-8 border">
          <div className="mb-6">
            <h2 className="mb-2">موقعیت مکانی</h2>
            <p className="text-muted-foreground">
              آدرس و موقعیت کسب‌و‌کار خود را مشخص کنید
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Mock Map */}
            <div className="space-y-2">
              <Label>انتخاب روی نقشه</Label>
              <div className="relative h-64 bg-muted rounded-xl overflow-hidden border">
                <div className="absolute inset-0 flex items-center justify-center">
                  <div className="text-center">
                    <MapPin className="w-12 h-12 text-primary mx-auto mb-2" />
                    <p className="text-sm text-muted-foreground">
                      کلیک کنید تا موقعیت را روی نقشه انتخاب کنید
                    </p>
                  </div>
                </div>
                {/* Simple grid pattern to simulate map */}
                <div className="absolute inset-0 opacity-10" style={{
                  backgroundImage: 'linear-gradient(0deg, #000 1px, transparent 1px), linear-gradient(90deg, #000 1px, transparent 1px)',
                  backgroundSize: '20px 20px'
                }}></div>
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="province">
                  استان <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="province"
                  value={formData.province}
                  onChange={(e) => setFormData({ ...formData, province: e.target.value })}
                  placeholder="مثال: تهران"
                  className={errors.province ? "border-destructive" : ""}
                />
                {errors.province && (
                  <p className="text-sm text-destructive">{errors.province}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="city">
                  شهر <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="city"
                  value={formData.city}
                  onChange={(e) => setFormData({ ...formData, city: e.target.value })}
                  placeholder="مثال: تهران"
                  className={errors.city ? "border-destructive" : ""}
                />
                {errors.city && (
                  <p className="text-sm text-destructive">{errors.city}</p>
                )}
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="address">
                آدرس دقیق <span className="text-destructive">*</span>
              </Label>
              <Input
                id="address"
                value={formData.address}
                onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                placeholder="مثال: خیابان ولیعصر، کوچه پنجم، پلاک ۱۲"
                className={errors.address ? "border-destructive" : ""}
              />
              {errors.address && (
                <p className="text-sm text-destructive">{errors.address}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="postalCode">کد پستی (اختیاری)</Label>
              <Input
                id="postalCode"
                value={formData.postalCode}
                onChange={(e) => setFormData({ ...formData, postalCode: e.target.value })}
                placeholder="1234567890"
                dir="ltr"
                className="text-left"
              />
            </div>

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
