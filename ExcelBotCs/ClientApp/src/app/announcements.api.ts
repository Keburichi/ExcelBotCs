import {Announcement} from "@/app/announcements.types";
import {http} from "@/services/http";

export const AnnouncementsApi = {
    list: () => http<Announcement[]>('/api/home/announcements')
}