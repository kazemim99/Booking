import { Button } from "../ui/button";
import { CheckCircle2, Sparkles } from "lucide-react";

interface CompletionProps {
  onGoToDashboard: () => void;
}

export function Completion({ onGoToDashboard }: CompletionProps) {
  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-gradient-to-br from-primary/5 to-accent/20">
      <div className="w-full max-w-md">
        <div className="bg-card rounded-2xl shadow-lg p-8 text-center">
          {/* Success Icon */}
          <div className="relative mb-6">
            <div className="w-24 h-24 bg-primary/10 rounded-full flex items-center justify-center mx-auto">
              <CheckCircle2 className="w-16 h-16 text-primary" />
            </div>
            <Sparkles className="w-6 h-6 text-primary absolute top-0 right-1/3 animate-pulse" />
            <Sparkles className="w-5 h-5 text-primary absolute bottom-2 left-1/4 animate-pulse delay-75" />
          </div>

          {/* Title & Message */}
          <h1 className="mb-3">تبریک می‌گوییم!</h1>
          <p className="text-muted-foreground mb-8">
            ثبت‌نام شما با موفقیت انجام شد. اکنون می‌توانید از تمامی امکانات پنل استفاده کنید
          </p>

          {/* Stats Cards */}
          <div className="grid grid-cols-3 gap-3 mb-8">
            <div className="p-3 bg-muted/50 rounded-lg">
              <div className="text-2xl mb-1">✓</div>
              <p className="text-xs text-muted-foreground">پروفایل کامل</p>
            </div>
            <div className="p-3 bg-muted/50 rounded-lg">
              <div className="text-2xl mb-1">🎯</div>
              <p className="text-xs text-muted-foreground">آماده فعالیت</p>
            </div>
            <div className="p-3 bg-muted/50 rounded-lg">
              <div className="text-2xl mb-1">🚀</div>
              <p className="text-xs text-muted-foreground">شروع موفق</p>
            </div>
          </div>

          {/* CTA Button */}
          <Button onClick={onGoToDashboard} className="w-full mb-4" size="lg">
            ورود به داشبورد
          </Button>

          {/* Next Steps */}
          <div className="mt-6 p-4 bg-accent/20 rounded-lg text-right">
            <p className="text-sm font-medium mb-2">گام‌های بعدی:</p>
            <ul className="text-sm text-muted-foreground space-y-1">
              <li>• بررسی و تکمیل اطلاعات پروفایل</li>
              <li>• افزودن نوبت‌های جدید</li>
              <li>• دعوت از مشتریان</li>
            </ul>
          </div>
        </div>

        {/* Additional Help */}
        <div className="mt-6 text-center">
          <p className="text-sm text-muted-foreground">
            نیاز به راهنمایی دارید؟{" "}
            <a href="#" className="text-primary hover:underline">
              مشاهده راهنمای کامل
            </a>
          </p>
        </div>
      </div>
    </div>
  );
}
