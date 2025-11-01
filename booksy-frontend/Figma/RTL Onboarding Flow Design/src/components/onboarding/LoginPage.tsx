import { useState } from "react";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";

interface LoginPageProps {
  onLogin: (phoneNumber: string) => void;
}

export function LoginPage({ onLogin }: LoginPageProps) {
  const [phoneNumber, setPhoneNumber] = useState("");
  const [error, setError] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError("");

    if (!phoneNumber) {
      setError("لطفاً شماره موبایل خود را وارد کنید");
      return;
    }

    // Simple validation for Iranian phone numbers
    const phoneRegex = /^09\d{9}$/;
    if (!phoneRegex.test(phoneNumber)) {
      setError("شماره موبایل وارد شده معتبر نیست");
      return;
    }

    onLogin(phoneNumber);
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-gradient-to-br from-primary/5 to-accent/20">
      <div className="w-full max-w-md">
        <div className="bg-card rounded-2xl shadow-lg p-8">
          <div className="text-center mb-8">
            <div className="w-16 h-16 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-8 h-8 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
              </svg>
            </div>
            <h1 className="mb-2">خوش آمدید</h1>
            <p className="text-muted-foreground">
              برای ورود به پنل ارائه‌دهندگان، شماره موبایل خود را وارد کنید
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="phone">شماره موبایل</Label>
              <Input
                id="phone"
                type="tel"
                placeholder="09123456789"
                value={phoneNumber}
                onChange={(e) => setPhoneNumber(e.target.value)}
                className="text-left bg-input-background border"
                dir="ltr"
              />
              {error && (
                <p className="text-sm text-destructive">{error}</p>
              )}
            </div>

            <Button type="submit" className="w-full">
              دریافت کد
            </Button>
          </form>

          <p className="text-center text-sm text-muted-foreground mt-6">
            با ورود به سیستم، شما{" "}
            <a href="#" className="text-primary hover:underline">
              قوانین و مقررات
            </a>{" "}
            را می‌پذیرید
          </p>
        </div>
      </div>
    </div>
  );
}
