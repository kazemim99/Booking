import { useState } from "react";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { ProgressIndicator } from "./ProgressIndicator";
import { Upload } from "lucide-react";

interface BusinessInfoProps {
  onNext: (data: any) => void;
}

export function BusinessInfo({ onNext }: BusinessInfoProps) {
  const [formData, setFormData] = useState({
    businessName: "",
    ownerName: "",
    email: "",
    secondaryPhone: "",
    logo: null as File | null,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [logoPreview, setLogoPreview] = useState<string | null>(null);

  const handleLogoChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setFormData({ ...formData, logo: file });
      const reader = new FileReader();
      reader.onloadend = () => {
        setLogoPreview(reader.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const validate = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.businessName) {
      newErrors.businessName = "نام کسب‌و‌کار الزامی است";
    }
    if (!formData.ownerName) {
      newErrors.ownerName = "نام و نام خانوادگی الزامی است";
    }
    if (!formData.email) {
      newErrors.email = "ایمیل الزامی است";
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = "ایمیل معتبر نیست";
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
        <ProgressIndicator currentStep={1} totalSteps={8} />

        <div className="bg-card rounded-2xl shadow-sm p-8 border">
          <div className="mb-6">
            <h2 className="mb-2">اطلاعات کسب‌و‌کار</h2>
            <p className="text-muted-foreground">
              لطفاً اطلاعات کسب‌و‌کار خود را وارد کنید
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="businessName">
                نام کسب‌و‌کار <span className="text-destructive">*</span>
              </Label>
              <Input
                id="businessName"
                value={formData.businessName}
                onChange={(e) => setFormData({ ...formData, businessName: e.target.value })}
                placeholder="مثال: آرایشگاه زیبا"
                className={errors.businessName ? "border-destructive" : ""}
              />
              {errors.businessName && (
                <p className="text-sm text-destructive">{errors.businessName}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="ownerName">
                نام و نام خانوادگی مالک <span className="text-destructive">*</span>
              </Label>
              <Input
                id="ownerName"
                value={formData.ownerName}
                onChange={(e) => setFormData({ ...formData, ownerName: e.target.value })}
                placeholder="مثال: علی احمدی"
                className={errors.ownerName ? "border-destructive" : ""}
              />
              {errors.ownerName && (
                <p className="text-sm text-destructive">{errors.ownerName}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">
                ایمیل <span className="text-destructive">*</span>
              </Label>
              <Input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                placeholder="example@domain.com"
                dir="ltr"
                className={`text-left ${errors.email ? "border-destructive" : ""}`}
              />
              {errors.email && (
                <p className="text-sm text-destructive">{errors.email}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="secondaryPhone">شماره تماس دوم (اختیاری)</Label>
              <Input
                id="secondaryPhone"
                type="tel"
                value={formData.secondaryPhone}
                onChange={(e) => setFormData({ ...formData, secondaryPhone: e.target.value })}
                placeholder="09123456789"
                dir="ltr"
                className="text-left"
              />
            </div>

            <div className="space-y-2">
              <Label>آپلود لوگو (اختیاری)</Label>
              <div className="flex items-center gap-4">
                {logoPreview && (
                  <div className="w-20 h-20 rounded-lg overflow-hidden border">
                    <img src={logoPreview} alt="Logo preview" className="w-full h-full object-cover" />
                  </div>
                )}
                <label className="flex-1 cursor-pointer">
                  <div className="border-2 border-dashed border-border rounded-lg p-6 hover:border-primary transition-colors text-center">
                    <Upload className="w-8 h-8 mx-auto mb-2 text-muted-foreground" />
                    <p className="text-sm text-muted-foreground">
                      کلیک کنید یا فایل را اینجا بکشید
                    </p>
                  </div>
                  <input
                    type="file"
                    accept="image/*"
                    onChange={handleLogoChange}
                    className="hidden"
                  />
                </label>
              </div>
            </div>

            <Button type="submit" className="w-full">
              بعدی
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
}
