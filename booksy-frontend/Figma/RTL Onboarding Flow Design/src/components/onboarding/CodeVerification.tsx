import { useState, useRef } from "react";
import { Button } from "../ui/button";
import { Input } from "../ui/input";

interface CodeVerificationProps {
  phoneNumber: string;
  onVerify: () => void;
  onBack: () => void;
}

export function CodeVerification({ phoneNumber, onVerify, onBack }: CodeVerificationProps) {
  const [code, setCode] = useState(["", "", "", "", ""]);
  const [error, setError] = useState("");
  const inputRefs = useRef<(HTMLInputElement | null)[]>([]);

  const handleChange = (index: number, value: string) => {
    if (value.length > 1) return;
    
    const newCode = [...code];
    newCode[index] = value;
    setCode(newCode);
    setError("");

    // Move to next input
    if (value && index < 4) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handleKeyDown = (index: number, e: React.KeyboardEvent) => {
    if (e.key === "Backspace" && !code[index] && index > 0) {
      inputRefs.current[index - 1]?.focus();
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const fullCode = code.join("");

    if (fullCode.length !== 5) {
      setError("لطفاً کد 5 رقمی را وارد کنید");
      return;
    }

    // Mock verification - in real app, this would call an API
    if (fullCode === "12345") {
      onVerify();
    } else {
      setError("کد وارد شده صحیح نیست");
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4 bg-gradient-to-br from-primary/5 to-accent/20">
      <div className="w-full max-w-md">
        <div className="bg-card rounded-2xl shadow-lg p-8">
          <div className="text-center mb-8">
            <div className="w-16 h-16 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-8 h-8 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
              </svg>
            </div>
            <h1 className="mb-2">تایید کد</h1>
            <p className="text-muted-foreground">
              کد 5 رقمی ارسال شده به شماره{" "}
              <span className="font-medium text-foreground" dir="ltr">{phoneNumber}</span>{" "}
              را وارد کنید
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="flex gap-3 justify-center" dir="ltr">
              {code.map((digit, index) => (
                <Input
                  key={index}
                  ref={(el) => (inputRefs.current[index] = el)}
                  type="text"
                  inputMode="numeric"
                  pattern="[0-9]*"
                  maxLength={1}
                  value={digit}
                  onChange={(e) => handleChange(index, e.target.value)}
                  onKeyDown={(e) => handleKeyDown(index, e)}
                  className="w-14 h-14 text-center text-xl bg-input-background border"
                />
              ))}
            </div>

            {error && (
              <p className="text-sm text-destructive text-center">{error}</p>
            )}

            <Button type="submit" className="w-full">
              تایید کد
            </Button>

            <div className="text-center space-y-2">
              <button
                type="button"
                className="text-sm text-primary hover:underline"
                onClick={() => {
                  setCode(["", "", "", "", ""]);
                  setError("");
                }}
              >
                ارسال مجدد کد
              </button>
              <br />
              <button
                type="button"
                className="text-sm text-muted-foreground hover:text-foreground"
                onClick={onBack}
              >
                بازگشت به صفحه ورود
              </button>
            </div>
          </form>

          <div className="mt-6 p-4 bg-muted/50 rounded-lg">
            <p className="text-xs text-muted-foreground text-center">
              💡 برای تست: کد <span className="font-medium">12345</span> را وارد کنید
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
