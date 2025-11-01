import { useState } from "react";
import { LoginPage } from "./components/onboarding/LoginPage";
import { CodeVerification } from "./components/onboarding/CodeVerification";
import { BusinessInfo } from "./components/onboarding/BusinessInfo";
import { CategorySelection } from "./components/onboarding/CategorySelection";
import { Location } from "./components/onboarding/Location";
import { Services } from "./components/onboarding/Services";
import { Staff } from "./components/onboarding/Staff";
import { WorkingHours } from "./components/onboarding/WorkingHours";
import { Gallery } from "./components/onboarding/Gallery";
import { OptionalFeedback } from "./components/onboarding/OptionalFeedback";
import { Completion } from "./components/onboarding/Completion";

type OnboardingStep =
  | "login"
  | "verification"
  | "businessInfo"
  | "category"
  | "location"
  | "services"
  | "staff"
  | "workingHours"
  | "gallery"
  | "feedback"
  | "completion";

interface OnboardingData {
  phoneNumber?: string;
  businessInfo?: any;
  category?: any;
  location?: any;
  services?: any;
  staff?: any;
  workingHours?: any;
  gallery?: any;
  feedback?: any;
}

export default function App() {
  const [currentStep, setCurrentStep] =
    useState<OnboardingStep>("login");
  const [onboardingData, setOnboardingData] =
    useState<OnboardingData>({});

  const handleLogin = (phoneNumber: string) => {
    setOnboardingData({ ...onboardingData, phoneNumber });
    setCurrentStep("verification");
  };

  const handleVerification = () => {
    setCurrentStep("businessInfo");
  };

  const handleBusinessInfo = (data: any) => {
    setOnboardingData({
      ...onboardingData,
      businessInfo: data,
    });
    setCurrentStep("category");
  };

  const handleCategory = (data: any) => {
    setOnboardingData({ ...onboardingData, category: data });
    setCurrentStep("location");
  };

  const handleLocation = (data: any) => {
    setOnboardingData({ ...onboardingData, location: data });
    setCurrentStep("services");
  };

  const handleServices = (data: any) => {
    setOnboardingData({ ...onboardingData, services: data });
    setCurrentStep("staff");
  };

  const handleStaff = (data: any) => {
    setOnboardingData({ ...onboardingData, staff: data });
    setCurrentStep("workingHours");
  };

  const handleWorkingHours = (data: any) => {
    setOnboardingData({
      ...onboardingData,
      workingHours: data,
    });
    setCurrentStep("gallery");
  };

  const handleGallery = (data: any) => {
    setOnboardingData({ ...onboardingData, gallery: data });
    setCurrentStep("feedback");
  };

  const handleFeedback = (data: any) => {
    setOnboardingData({ ...onboardingData, feedback: data });
    console.log("Complete Onboarding Data:", {
      ...onboardingData,
      feedback: data,
    });
    setCurrentStep("completion");
  };

  const handleGoToDashboard = () => {
    console.log("Navigating to dashboard...");
    // In a real app, this would navigate to the dashboard
    alert("در یک برنامه واقعی، شما به داشبورد هدایت می‌شوید");
  };

  return (
    <div className="min-h-screen">
      {currentStep === "login" && (
        <LoginPage onLogin={handleLogin} />
      )}

      {currentStep === "verification" && (
        <CodeVerification
          phoneNumber={onboardingData.phoneNumber || ""}
          onVerify={handleVerification}
          onBack={() => setCurrentStep("login")}
        />
      )}

      {currentStep === "businessInfo" && (
        <BusinessInfo onNext={handleBusinessInfo} />
      )}

      {currentStep === "category" && (
        <CategorySelection
          onNext={handleCategory}
          onBack={() => setCurrentStep("businessInfo")}
        />
      )}

      {currentStep === "location" && (
        <Location
          onNext={handleLocation}
          onBack={() => setCurrentStep("category")}
        />
      )}

      {currentStep === "services" && (
        <Services
          onNext={handleServices}
          onBack={() => setCurrentStep("location")}
        />
      )}

      {currentStep === "staff" && (
        <Staff
          onNext={handleStaff}
          onBack={() => setCurrentStep("services")}
        />
      )}

      {currentStep === "workingHours" && (
        <WorkingHours
          onNext={handleWorkingHours}
          onBack={() => setCurrentStep("staff")}
        />
      )}

      {currentStep === "gallery" && (
        <Gallery
          onNext={handleGallery}
          onBack={() => setCurrentStep("workingHours")}
        />
      )}

      {currentStep === "feedback" && (
        <OptionalFeedback
          onNext={handleFeedback}
          onBack={() => setCurrentStep("gallery")}
        />
      )}

      {currentStep === "completion" && (
        <Completion onGoToDashboard={handleGoToDashboard} />
      )}
    </div>
  );
}