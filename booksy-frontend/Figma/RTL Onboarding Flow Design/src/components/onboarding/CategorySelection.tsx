import { useState } from "react";
import { Button } from "../ui/button";
import { Label } from "../ui/label";
import { ProgressIndicator } from "./ProgressIndicator";
import { Check } from "lucide-react";

interface CategorySelectionProps {
  onNext: (data: any) => void;
  onBack: () => void;
}

const categories = [
  { id: "salon", name: "آرایشگاه و سالن زیبایی", icon: "✂️" },
  { id: "spa", name: "اسپا و ماساژ", icon: "💆" },
  { id: "clinic", name: "کلینیک پوست و مو", icon: "🏥" },
  { id: "nails", name: "آرایش ناخن", icon: "💅" },
  { id: "makeup", name: "آرایشگری و میکاپ", icon: "💄" },
  { id: "fitness", name: "باشگاه و فیتنس", icon: "💪" },
  { id: "tattoo", name: "تاتو و خالکوبی", icon: "🎨" },
  { id: "dental", name: "دندانپزشکی زیبایی", icon: "🦷" },
  { id: "other", name: "سایر", icon: "📋" },
];

export function CategorySelection({ onNext, onBack }: CategorySelectionProps) {
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [error, setError] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCategory) {
      setError("لطفاً یک دسته‌بندی انتخاب کنید");
      return;
    }
    onNext({ category: selectedCategory });
  };

  return (
    <div className="min-h-screen bg-background p-4 py-8">
      <div className="max-w-2xl mx-auto">
        <ProgressIndicator currentStep={2} totalSteps={8} />

        <div className="bg-card rounded-2xl shadow-sm p-8 border">
          <div className="mb-6">
            <h2 className="mb-2">انتخاب دسته‌بندی</h2>
            <p className="text-muted-foreground">
              لطفاً دسته‌بندی کسب‌و‌کار خود را انتخاب کنید
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {categories.map((category) => (
                <button
                  key={category.id}
                  type="button"
                  onClick={() => {
                    setSelectedCategory(category.id);
                    setError("");
                  }}
                  className={`relative p-4 rounded-xl border-2 transition-all text-right hover:border-primary/50 ${
                    selectedCategory === category.id
                      ? "border-primary bg-primary/5"
                      : "border-border bg-card"
                  }`}
                >
                  <div className="flex items-center gap-3">
                    <div className="text-2xl">{category.icon}</div>
                    <span className="flex-1">{category.name}</span>
                    {selectedCategory === category.id && (
                      <div className="w-6 h-6 rounded-full bg-primary flex items-center justify-center">
                        <Check className="w-4 h-4 text-primary-foreground" />
                      </div>
                    )}
                  </div>
                </button>
              ))}
            </div>

            {error && (
              <p className="text-sm text-destructive">{error}</p>
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
