import type { RouteRecordRaw } from 'vue-router'
import { createRouter, createWebHistory } from 'vue-router'
import HomeView from '@/app/HomeView.vue'
import AdminView from '@/components/adminPanel/AdminView.vue'
import LoginView from '@/components/auth/LoginView.vue'
import CreateEventView from '@/components/events/CreateEventView.vue'
import EventsListView from '@/components/events/EventsListView.vue'
import FcMembersListView from '@/components/fcMembers/FcMembersListView.vue'
import FightsView from '@/components/fights/FightsView.vue'
import LotteryView from '@/components/lottery/LotteryView.vue'
import MemberEditView from '@/components/members/MemberEditView.vue'
import ProfileView from '@/components/profile/ProfileView.vue'
import { useAuth } from '@/composables/useAuth'

const routes: RouteRecordRaw[] = [
  { path: '/login', name: 'login', component: LoginView },
  { path: '/', redirect: '/home' },
  { path: '/home', name: 'home', component: HomeView, meta: { requiresAuth: true } },

  // Member routes
  { path: '/members', name: 'members', component: FcMembersListView, meta: { requiresAuth: true } },
  { path: '/members/:id', name: 'member-edit', component: MemberEditView, meta: { requiresAuth: true } },

  // Event routes
  { path: '/events', name: 'events', component: EventsListView, meta: { requiresAuth: true } },
  {
    path: '/events/new',
    name: 'event-create',
    component: CreateEventView,
    meta: { requiresAuth: true, requiresAdmin: true },
  },
  {
    path: '/events/:id',
    name: 'event-edit',
    component: CreateEventView,
    meta: { requiresAuth: true, requiresAdmin: true },
  },

  // Fight routes
  { path: '/fights', name: 'fights', component: FightsView, meta: { requiresAuth: true } },

  // Profile routes
  { path: '/profile', name: 'profile', component: ProfileView, meta: { requiresAuth: true } },

  // Lottery routes
  { path: '/lottery', name: 'lottery', component: LotteryView, meta: { requiresAuth: true } },
  { path: '/lottery/new', name: 'lottery-create', component: ProfileView, meta: { requiresAuth: true } },

  // Admin routes
  { path: '/admin', name: 'admin', component: AdminView, meta: { requiresAuth: true, requiresAdmin: true } },
  { path: '/admin/roles', name: 'admin-roles', component: AdminView, meta: { requiresAuth: true, requiresAdmin: true } },

  // Redirect everything unknown to NotFound
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/app/NotFound.vue') },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})

router.beforeEach(async (to) => {
  const { ensureAuth, loadMe, isAdmin } = useAuth()

  if (to.meta.requiresAuth) {
    const ok = await ensureAuth()
    if (!ok)
      return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresAdmin) {
    await loadMe()
    if (!isAdmin.value) {
      return { name: 'home' }
    }
  }
})
