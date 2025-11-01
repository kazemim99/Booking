import { useState } from "react";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { ProgressIndicator } from "./ProgressIndicator";
import { Plus, Trash2, Edit2 } from "lucide-react";

interface Service {
  id: string;
  name: string;
  price: string;
  duration: string;
}

interface ServicesProps {
  onNext: (data: any) => void;
  onBack: () => void;
}

export function Services({ onNext, onBack }: ServicesProps) {
  const [services, setServices] = useState<Service[]>([]);
  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    name: "",
    price: "",
    duration: "",
  });
  const [error, setError] = useState("");

  const handleAddService = () => {
    if (!formData.name || !formData.price || !formData.duration) {
      setError("لطفاً تمام فیلدها را پر کنید");
      return;
    }

    if (editingId) {
      setServices(services.map(s => 
        s.id === editingId ? { ...formData, id: editingId } : s
      ));
      setEditingId(null);
    } else {
      setServices([...services, { ...formData, id: Date.now().toString() }]);
    }

    setFormData({ name: "", price: "", duration: "" });
    setIsAdding(false);
    setError("");
  };

  const handleEdit = (service: Service) => {
    setFormData({
      name: service.name,
      price: service.price,
      duration: service.duration,
    });
    setEditingId(service.id);
    setIsAdding(true);
  };

  const handleDelete = (id: string) => {
    setServices(services.filter(s => s.id !== id));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (services.length === 0) {
      setError("لطفاً حداقل یک خدمت اضافه کنید");
      return;
    }
    onNext({ services });
  };

  return (
    <div className="min-h-screen bg-background p-4 py-8">
      <div className="max-w-2xl mx-auto">
        <ProgressIndicator currentStep={4} totalSteps={8} />

        <div className="bg-card rounded-2xl shadow-sm p-8 border">
          <div className="mb-6">
            <h2 className="mb-2">خدمات</h2>
            <p className="text-muted-foreground">
              خدماتی که ارائه می‌دهید را اضافه کنید
            </p>
          </div>

          <div className="space-y-6">
            {/* Service List */}
            {services.length > 0 && (
              <div className="space-y-3">
                {services.map((service) => (
                  <div
                    key={service.id}
                    className="flex items-center gap-3 p-4 bg-muted/50 rounded-lg border"
                  >
                    <div className="flex-1">
                      <h4 className="mb-1">{service.name}</h4>
                      <p className="text-sm text-muted-foreground">
                        قیمت: {service.price} تومان • مدت: {service.duration} دقیقه
                      </p>
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => handleEdit(service)}
                    >
                      <Edit2 className="w-4 h-4" />
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => handleDelete(service.id)}
                    >
                      <Trash2 className="w-4 h-4 text-destructive" />
                    </Button>
                  </div>
                ))}
              </div>
            )}

            {/* Add/Edit Form */}
            {isAdding ? (
              <div className="space-y-4 p-4 bg-accent/20 rounded-lg border border-primary/20">
                <div className="space-y-2">
                  <Label htmlFor="serviceName">نام خدمت</Label>
                  <Input
                    id="serviceName"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    placeholder="مثال: اصلاح مو"
                  />
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="price">قیمت (تومان)</Label>
                    <Input
                      id="price"
                      type="number"
                      value={formData.price}
                      onChange={(e) => setFormData({ ...formData, price: e.target.value })}
                      placeholder="۱۰۰۰۰۰"
                      dir="ltr"
                      className="text-left"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="duration">مدت زمان (دقیقه)</Label>
                    <Input
                      id="duration"
                      type="number"
                      value={formData.duration}
                      onChange={(e) => setFormData({ ...formData, duration: e.target.value })}
                      placeholder="۳۰"
                      dir="ltr"
                      className="text-left"
                    />
                  </div>
                </div>

                <div className="flex gap-2">
                  <Button
                    type="button"
                    onClick={handleAddService}
                    className="flex-1"
                  >
                    {editingId ? "ویرایش" : "افزودن"}
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      setIsAdding(false);
                      setEditingId(null);
                      setFormData({ name: "", price: "", duration: "" });
                      setError("");
                    }}
                    className="flex-1"
                  >
                    لغو
                  </Button>
                </div>
              </div>
            ) : (
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsAdding(true)}
                className="w-full border-dashed"
              >
                <Plus className="w-4 h-4 ml-2" />
                افزودن خدمت جدید
              </Button>
            )}

            {error && (
              <p className="text-sm text-destructive">{error}</p>
            )}

            <form onSubmit={handleSubmit}>
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
    </div>
  );
}
