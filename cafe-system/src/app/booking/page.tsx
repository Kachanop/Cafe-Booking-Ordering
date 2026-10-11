import Image from "next/image";
import Link from "next/link";
import Navbar from "@/components/Navbar";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";

// Mock Data สำหรับศิลปิน/ช่างภาพ
const mockArtists = [
  {
    id: 1,
    name: "คุณเอก (Aek)",
    role: "ช่างภาพ (Photographer)",
    image: "https://i.pravatar.cc/150?u=aek",
    isAvailable: true,
    tags: ["Portrait", "Graduation", "Cafe"],
  },
  {
    id: 2,
    name: "คุณส้ม (Som)",
    role: "ช่างแต่งหน้า (Makeup Artist)",
    image: "https://i.pravatar.cc/150?u=som",
    isAvailable: true,
    tags: ["Bridal", "Fashion", "Everyday Look"],
  },
  {
    id: 3,
    name: "คุณบอย (Boy)",
    role: "ช่างภาพ (Photographer)",
    image: "https://i.pravatar.cc/150?u=boy",
    isAvailable: false,
    tags: ["Product", "Film", "Minimal"],
  },
];

// Mock Data สำหรับรอบเวลา
const timeSlots = ["10:00 - 12:00", "13:00 - 15:00", "15:00 - 17:00", "17:00 - 19:00"];

export default function BookingPage() {
  return (
    <main className="min-h-screen bg-coffee-50 pb-12 font-sans">
      
      {/* Sticky Navigation Menu */}
      <Navbar />

      {/* Breadcrumb / Back Button */}
      <div className="max-w-6xl mx-auto px-4 mt-8 mb-4">
        <Link href="/">
          <Button variant="ghost" className="text-coffee-700 hover:text-coffee-900 hover:bg-coffee-100 rounded-full">
            &larr; กลับหน้าหลัก
          </Button>
        </Link>
      </div>

      {/* Artists Section */}
      <section className="max-w-6xl mx-auto px-4">
        <div className="flex justify-between items-center mb-8">
          <h2 className="text-3xl font-extrabold text-coffee-900">เลือกช่างภาพ / ช่างแต่งหน้า</h2>
          <Button variant="outline" className="border-coffee-300 text-coffee-700 hover:bg-coffee-100 rounded-full px-6">
            ดูทั้งหมด
          </Button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          {mockArtists.map((artist) => (
            <Card key={artist.id} className="hover:shadow-xl transition-all border-coffee-100 bg-white overflow-hidden rounded-2xl">
              <CardHeader className="flex flex-row items-center gap-4 pb-4 pt-6">
                <Avatar className="w-20 h-20 border-4 border-coffee-50 shadow-sm">
                  <AvatarImage src={artist.image} alt={artist.name} />
                  <AvatarFallback className="bg-coffee-100 text-coffee-900 text-xl font-bold">{artist.name[0]}</AvatarFallback>
                </Avatar>
                <div>
                  <CardTitle className="text-xl text-coffee-900 font-bold">{artist.name}</CardTitle>
                  <CardDescription className="text-sm font-semibold text-coffee-500 mt-1">
                    {artist.role}
                  </CardDescription>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-2 mb-6">
                  {artist.tags.map((tag) => (
                    <Badge key={tag} variant="secondary" className="bg-coffee-50 text-coffee-700 hover:bg-coffee-300 hover:text-white transition-colors">
                      {tag}
                    </Badge>
                  ))}
                </div>
                <div className="flex items-center gap-2 bg-coffee-50 p-3 rounded-lg">
                  <div
                    className={`w-3 h-3 rounded-full ${
                      artist.isAvailable ? "bg-green-500" : "bg-red-500"
                    } shadow-sm`}
                  ></div>
                  <span className="text-sm font-semibold text-coffee-900">
                    {artist.isAvailable ? "มีคิวว่างวันนี้" : "คิวเต็มแล้ว"}
                  </span>
                </div>
              </CardContent>
              <CardFooter className="pb-6">
                <Button className="w-full bg-coffee-700 hover:bg-coffee-900 text-white rounded-xl py-6 text-md font-semibold shadow-md" disabled={!artist.isAvailable}>
                  {artist.isAvailable ? "ดู Portfolio & จองคิว" : "คิวเต็ม"}
                </Button>
              </CardFooter>
            </Card>
          ))}
        </div>
      </section>

      {/* Booking Time Slots Mockup */}
      <section className="max-w-6xl mx-auto px-4 mt-16">
        <div className="bg-white p-10 rounded-3xl border border-coffee-100 shadow-lg relative overflow-hidden">
          <div className="absolute top-0 right-0 w-64 h-64 bg-coffee-50 rounded-full blur-3xl -mr-20 -mt-20 opacity-50"></div>
          <h2 className="text-3xl font-extrabold text-coffee-900 mb-8 relative z-10">จำลองการเลือกรอบเวลา (Time Slots)</h2>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-6 relative z-10">
            {timeSlots.map((slot, index) => (
               <Button
               key={index}
               variant={index === 1 ? "default" : "outline"}
               className={`h-24 text-lg border-2 rounded-2xl transition-all ${
                 index === 1 
                   ? "bg-coffee-500 hover:bg-coffee-700 text-white border-coffee-500 shadow-md scale-105" 
                   : "border-coffee-100 text-coffee-700 hover:border-coffee-300 hover:bg-coffee-50 bg-white"
               } ${index === 3 ? "opacity-50 cursor-not-allowed border-coffee-50 text-gray-400" : ""}`}
             >
               <div className="flex flex-col items-center justify-center gap-1">
                 <span className="font-bold">{slot}</span>
                 {index === 3 && <span className="text-xs text-red-500 font-extrabold bg-red-50 px-2 py-1 rounded-md">คิวเต็ม</span>}
               </div>
             </Button>
            ))}
          </div>
        </div>
      </section>
    </main>
  );
}
