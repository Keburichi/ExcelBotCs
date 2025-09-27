import {createRouter, createWebHistory, type RouteRecordRaw} from 'vue-router'
import LoginView from '@/features/auth/LoginView.vue'
import MembersListView from '@/features/members/MembersListView.vue'
import MemberEditView from '@/features/members/MemberEditView.vue'
import {useAuth} from '@/features/auth/useAuth'
import EventsListView from "@/features/events/EventsListView.vue";
import CreateEventView from "@/features/events/CreateEventView.vue";
import ProfileView from "@/features/profile/ProfileView.vue";
import HomeView from "@/app/HomeView.vue";
import AdminView from "@/features/adminPanel/AdminView.vue";
import FightsView from "@/features/fights/FightsView.vue";
import FcMembersListView from "@/features/fcMembers/FcMembersListView.vue";

const routes: RouteRecordRaw[] = [
    {path: '/login', name: 'login', component: LoginView},
    {path: '/', redirect: '/home'},
    {path: '/home', name: 'home', component: HomeView, meta: {requiresAuth: true}},

    // Member routes
    {path: '/members', name: 'members', component: FcMembersListView, meta: {requiresAuth: true}},
    {path: '/members/:id', name: 'member-edit', component: MemberEditView, meta: {requiresAuth: true}},

    // Event routes
    {path: '/events', name: 'events', component: EventsListView, meta: {requiresAuth: true}},
    {
        path: '/events/new',
        name: 'event-create',
        component: CreateEventView,
        meta: {requiresAuth: true, requiresAdmin: true}
    },

    // Fight routes
    {path: '/fights', name: 'fights', component: FightsView, meta: {requiresAuth: true}},

    // Profile routes
    {path: '/profile', name: 'profile', component: ProfileView, meta: {requiresAuth: true}},
    
    // Lottery routes
    {path: '/lottery', name: 'lottery', component: ProfileView, meta: {requiresAuth: true}},
    {path: '/lottery/new', name: 'lottery-create', component: ProfileView, meta: {requiresAuth: true}},

    // Admin routes
    {path: '/admin', name: 'admin', component: AdminView, meta: {requiresAuth: true, requiresAdmin: true}},
    {path: '/admin/roles', name: 'admin-roles', component: AdminView, meta: {requiresAuth: true, requiresAdmin: true}},

    // Redirect everything unknown to NotFound
    {path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/app/NotFound.vue')}
]

export const router = createRouter({
    history: createWebHistory(),
    routes
})

router.beforeEach(async (to) => {
    const {ensureAuth, loadMe, isAdmin} = useAuth()

    if (to.meta.requiresAuth) {
        const ok = await ensureAuth()
        if (!ok) return {name: 'login', query: {redirect: to.fullPath}}
    }

    if (to.meta.requiresAdmin) {
        await loadMe()
        if (!isAdmin.value) {
            return {name: 'home'}
        }
    }
})