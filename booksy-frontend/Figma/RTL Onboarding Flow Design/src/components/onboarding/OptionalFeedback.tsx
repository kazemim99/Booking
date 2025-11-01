import { useState } from "react";
import { Button } from "../ui/button";
import { Label } from "../ui/label";
import { Textarea } from "../ui/textarea";
import { ProgressIndicator } from "./ProgressIndicator";
import { MessageSquare } from "lucide-react";

interface OptionalFeedbackProps {
  onNext: (data: any) => void;
  onBack: () => void;
}

export function OptionalFeedback({ onNext, onBack }: OptionalFeedbackProps) {
  const [feedback, setFeedback] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onNext({ feedback });
  };

  return (
    <div className="min-h-screen bg-background p-4 py-8">
      <div className="max-w-2xl mx-auto">
        <ProgressIndicator currentStep={8} totalSteps={8} />

        <div className="bg-card rounded-2xl shadow-sm p-8 border">
          <div className="mb-6 text-center">
            <div className="w-16 h-16 bg-primary/10 rounded-full flex items-center justify-center mx-auto mb-4">
              <MessageSquare className="w-8 h-8 text-primary" />
            </div>
            <h2 className="mb-2">نظر شما برای ما مهم است</h2>
            <p className="text-muted-foreground">
              چطور می‌توانیم بیشتر به شما کمک کنیم؟
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="feedback">
                نظرات و پیشنهادات خود را با ما در میان بگذارید (اختیاری)
              </Label>
              <Textarea
                id="feedback"
                value={feedback}
                onChange={(e) => setFeedback(e.target.value)}
                placeholder="لطفاً نظرات، پیشنهادات یا موارد خاصی که نیاز دارید را بنویسید..."
                className="min-h-[150px] resize-none"
              />
              <p className="text-xs text-muted-foreground">
                این بخش کاملاً اختیاری است و می‌توانید آن را خالی بگذارید
              </p>
            </div>

            <div className="p-4 bg-accent/20 rounded-lg">
              <p className="text-sm">
                💡 <span className="font-medium">نکته:</span> بازخورد شما به ما کمک می‌کند تا خدمات بهتری ارائه دهیم
              </p>
            </div>

            <div className="flex gap-3">
              <Button type="button" variant="outline" onClick={onBack} className="flex-1">
                قبلی
              </Button>
              <Button type="submit" className="flex-1">
                پایان
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
