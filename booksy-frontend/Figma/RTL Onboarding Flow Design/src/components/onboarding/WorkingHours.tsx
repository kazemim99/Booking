import { useState } from "react";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Switch } from "../ui/switch";
import { ProgressIndicator } from "./ProgressIndicator";
import { Copy } from "lucide-react";

interface DaySchedule {
  isOpen: boolean;
  openTime: string;
  closeTime: string;
}

interface WorkingHoursProps {
  onNext: (data: any) => void;
  onBack: () => void;
}

const weekDays = [
  { id: "saturday", name: "شنبه" },
  { id: "sunday", name: "یکشنبه" },
  { id: "monday", name: "دوشنبه" },
  { id: "tuesday", name: "سه‌شنبه" },
  { id: "wednesday", name: "چهارشنبه" },
  { id: "thursday", name: "پنجشنبه" },
  { id: "friday", name: "جمعه" },
];

export function WorkingHours({ onNext, onBack }: WorkingHoursProps) {
  const [schedule, setSchedule] = useState<Record<string, DaySchedule>>({
    saturday: { isOpen: true, openTime: "09:00", closeTime: "18:00" },
    sunday: { isOpen: true, openTime: "09:00", closeTime: "18:00" },
    monday: { isOpen: true, openTime: "09:00", closeTime: "18:00" },
    tuesday: { isOpen: true, openTime: "09:00", closeTime: "18:00" },
    wednesday: { isOpen: true, openTime: "09:00", closeTime: "18:00" },
    thursday: { isOpen: true, openTime: "09:00", closeTime: "18:00" },
    friday: { isOpen: false, openTime: "09:00", closeTime: "18:00" },
  });

  const [copyFromDay, setCopyFromDay] = useState<string | null>(null);

  const handleToggleDay = (dayId: string) => {
    setSchedule({
      ...schedule,
      [dayId]: {
        ...schedule[dayId],
        isOpen: !schedule[dayId].isOpen,
      },
    });
  };

  const handleTimeChange = (dayId: string, field: "openTime" | "closeTime", value: string) => {
    setSchedule({
      ...schedule,
      [dayId]: {
        ...schedule[dayId],
        [field]: value,
      },
    });
  };

  const handleCopySchedule = (fromDayId: string) => {
    const fromSchedule = schedule[fromDayId];
    const newSchedule = { ...schedule };
    
    weekDays.forEach(day => {
      if (day.id !== fromDayId) {
        newSchedule[day.id] = {
          ...fromSchedule,
        };
      }
    });
    
    setSchedule(newSchedule);
    setCopyFromDay(null);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onNext({ schedule });
  };

  return (
    <div className="min-h-screen bg-background p-4 py-8">
      <div className="max-w-2xl mx-auto">
        <ProgressIndicator currentStep={6} totalSteps={8} />

        <div className="bg-card rounded-2xl shadow-sm p-8 border">
          <div className="mb-6">
            <h2 className="mb-2">ساعات کاری</h2>
            <p className="text-muted-foreground">
              روزها و ساعات کاری خود را مشخص کنید
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-4">
              {weekDays.map((day) => (
                <div
                  key={day.id}
                  className={`p-4 rounded-lg border transition-all ${
                    schedule[day.id].isOpen ? "bg-card" : "bg-muted/30"
                  }`}
                >
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-3">
                      <Switch
                        checked={schedule[day.id].isOpen}
                        onCheckedChange={() => handleToggleDay(day.id)}
                      />
                      <Label className="cursor-pointer" onClick={() => handleToggleDay(day.id)}>
                        {day.name}
                      </Label>
                    </div>
                    
                    {schedule[day.id].isOpen && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => handleCopySchedule(day.id)}
                      >
                        <Copy className="w-4 h-4 ml-1" />
                        کپی به سایر روزها
                      </Button>
                    )}
                  </div>

                  {schedule[day.id].isOpen && (
                    <div className="grid grid-cols-2 gap-4 pr-11">
                      <div className="space-y-2">
                        <Label className="text-sm text-muted-foreground">
                          ساعت شروع
                        </Label>
                        <Input
                          type="time"
                          value={schedule[day.id].openTime}
                          onChange={(e) => handleTimeChange(day.id, "openTime", e.target.value)}
                          dir="ltr"
                          className="text-left"
                        />
                      </div>
                      <div className="space-y-2">
                        <Label className="text-sm text-muted-foreground">
                          ساعت پایان
                        </Label>
                        <Input
                          type="time"
                          value={schedule[day.id].closeTime}
                          onChange={(e) => handleTimeChange(day.id, "closeTime", e.target.value)}
                          dir="ltr"
                          className="text-left"
                        />
                      </div>
                    </div>
                  )}
                </div>
              ))}
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
