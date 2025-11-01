import { useState } from "react";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { ProgressIndicator } from "./ProgressIndicator";
import { Plus, Trash2, Edit2, User } from "lucide-react";

interface StaffMember {
  id: string;
  name: string;
  phone: string;
  position: string;
}

interface StaffProps {
  onNext: (data: any) => void;
  onBack: () => void;
}

export function Staff({ onNext, onBack }: StaffProps) {
  const [staff, setStaff] = useState<StaffMember[]>([]);
  const [isAdding, setIsAdding] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [formData, setFormData] = useState({
    name: "",
    phone: "",
    position: "",
  });
  const [error, setError] = useState("");

  const handleAddStaff = () => {
    if (!formData.name || !formData.phone || !formData.position) {
      setError("لطفاً تمام فیلدها را پر کنید");
      return;
    }

    if (editingId) {
      setStaff(staff.map(s => 
        s.id === editingId ? { ...formData, id: editingId } : s
      ));
      setEditingId(null);
    } else {
      setStaff([...staff, { ...formData, id: Date.now().toString() }]);
    }

    setFormData({ name: "", phone: "", position: "" });
    setIsAdding(false);
    setError("");
  };

  const handleEdit = (member: StaffMember) => {
    setFormData({
      name: member.name,
      phone: member.phone,
      position: member.position,
    });
    setEditingId(member.id);
    setIsAdding(true);
  };

  const handleDelete = (id: string) => {
    setStaff(staff.filter(s => s.id !== id));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onNext({ staff });
  };

  return (
    <div className="min-h-screen bg-background p-4 py-8">
      <div className="max-w-2xl mx-auto">
        <ProgressIndicator currentStep={5} totalSteps={8} />

        <div className="bg-card rounded-2xl shadow-sm p-8 border">
          <div className="mb-6">
            <h2 className="mb-2">پرسنل</h2>
            <p className="text-muted-foreground">
              اطلاعات پرسنل و همکاران خود را اضافه کنید (اختیاری)
            </p>
          </div>

          <div className="space-y-6">
            {/* Staff List */}
            {staff.length > 0 && (
              <div className="space-y-3">
                {staff.map((member) => (
                  <div
                    key={member.id}
                    className="flex items-center gap-3 p-4 bg-muted/50 rounded-lg border"
                  >
                    <div className="w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center">
                      <User className="w-6 h-6 text-primary" />
                    </div>
                    <div className="flex-1">
                      <h4 className="mb-1">{member.name}</h4>
                      <p className="text-sm text-muted-foreground">
                        {member.position} • {member.phone}
                      </p>
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => handleEdit(member)}
                    >
                      <Edit2 className="w-4 h-4" />
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => handleDelete(member.id)}
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
                  <Label htmlFor="staffName">نام و نام خانوادگی</Label>
                  <Input
                    id="staffName"
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    placeholder="مثال: سارا محمدی"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="staffPosition">سمت</Label>
                  <Input
                    id="staffPosition"
                    value={formData.position}
                    onChange={(e) => setFormData({ ...formData, position: e.target.value })}
                    placeholder="مثال: آرایشگر"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="staffPhone">شماره تماس</Label>
                  <Input
                    id="staffPhone"
                    type="tel"
                    value={formData.phone}
                    onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                    placeholder="09123456789"
                    dir="ltr"
                    className="text-left"
                  />
                </div>

                <div className="flex gap-2">
                  <Button
                    type="button"
                    onClick={handleAddStaff}
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
                      setFormData({ name: "", phone: "", position: "" });
                      setError("");
                    }}
                    className="flex-1"
                  >
                    لغو
                  </Button>
                </div>

                {error && (
                  <p className="text-sm text-destructive">{error}</p>
                )}
              </div>
            ) : (
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsAdding(true)}
                className="w-full border-dashed"
              >
                <Plus className="w-4 h-4 ml-2" />
                افزودن پرسنل جدید
              </Button>
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
